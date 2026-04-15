using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace IT_Form.Application.Features.SharedMailbox
{
    public sealed record GetSharedMailboxReportPageQuery(int? Id, string? SearchString, int Page = 1) : IRequest<ProcessResult<SharedMailboxReportPageViewModel>>;
    public sealed record ExportSharedMailboxReportCsvQuery(int? Id, string? SearchString) : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class GetSharedMailboxReportPageQueryHandler : IRequestHandler<GetSharedMailboxReportPageQuery, ProcessResult<SharedMailboxReportPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetSharedMailboxReportPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<SharedMailboxReportPageViewModel>> Handle(GetSharedMailboxReportPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            string mailboxName = string.Empty;
            if (request.Id.HasValue)
            {
                var mailbox = await _context.SharedMailboxes
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        item.MailboxName,
                        CompanyIds = item.SharedMailboxCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (mailbox == null)
                {
                    return ProcessResult<SharedMailboxReportPageViewModel>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !mailbox.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<SharedMailboxReportPageViewModel>.Forbidden();
                }

                mailboxName = mailbox.MailboxName;
            }

            var page = request.Page < 1 ? 1 : request.Page;
            const int pageSize = 15;
            var report = await PaginatedList<SharedMailboxAccessReportViewModel>.CreateAsync(
                SharedMailboxReportBuilder.BuildReportQuery(_context, request.Id, request.SearchString, scopedCompanies, accessibleCompanyIds),
                page,
                pageSize);

            return ProcessResult<SharedMailboxReportPageViewModel>.Success(new SharedMailboxReportPageViewModel
            {
                Report = report,
                CurrentFilter = request.SearchString,
                MailboxId = request.Id,
                MailboxName = mailboxName
            });
        }
    }

    public sealed class ExportSharedMailboxReportCsvQueryHandler : IRequestHandler<ExportSharedMailboxReportCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportSharedMailboxReportCsvQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportSharedMailboxReportCsvQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            if (request.Id.HasValue)
            {
                var mailbox = await _context.SharedMailboxes
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        CompanyIds = item.SharedMailboxCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (mailbox == null)
                {
                    return ProcessResult<BinaryFilePayload>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !mailbox.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<BinaryFilePayload>.Forbidden();
                }
            }

            var reportData = await SharedMailboxReportBuilder.BuildReportQuery(_context, request.Id, request.SearchString, scopedCompanies, accessibleCompanyIds)
                .ToListAsync(cancellationToken);
            var builder = new StringBuilder();
            builder.AppendLine("Employee Name,Department,Branch,Mailbox Name,Mailbox Address,Request Type,Request ID,Date Granted");

            foreach (var item in reportData)
            {
                var empName = item.EmployeeName?.Replace("\"", "\"\"") ?? string.Empty;
                var dept = item.Department?.Replace("\"", "\"\"") ?? string.Empty;
                var branch = item.Branch?.Replace("\"", "\"\"") ?? string.Empty;
                var mbName = item.MailboxName?.Replace("\"", "\"\"") ?? string.Empty;
                var mbAddr = item.MailboxAddress?.Replace("\"", "\"\"") ?? string.Empty;
                var reqType = item.RequestType?.Replace("\"", "\"\"") ?? string.Empty;
                var dateGranted = item.DateGranted?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

                builder.AppendLine($"\"{empName}\",\"{dept}\",\"{branch}\",\"{mbName}\",\"{mbAddr}\",\"{reqType}\",\"{item.RequestId}\",\"{dateGranted}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var resultData = new byte[bom.Length + bytes.Length];
            Buffer.BlockCopy(bom, 0, resultData, 0, bom.Length);
            Buffer.BlockCopy(bytes, 0, resultData, bom.Length, bytes.Length);

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                resultData,
                "text/csv",
                $"shared_mailbox_access_report_{DateTime.Now:yyyyMMdd}.csv"));
        }
    }

    internal static class SharedMailboxReportBuilder
    {
        public static IQueryable<SharedMailboxAccessReportViewModel> BuildReportQuery(
            ApplicationDbContext context,
            int? mailboxId,
            string? searchString,
            IReadOnlyCollection<string>? scopedCompanies = null,
            IReadOnlyCollection<int>? accessibleCompanyIds = null)
        {
            var query = context.RequestSharedMailboxes
                .AsNoTracking()
                .Where(r => r.OnboardingRequest != null && r.OnboardingRequest.Status == RequestStatus.Completed);

            if (accessibleCompanyIds is { Count: > 0 })
            {
                query = query.Where(r => r.SharedMailbox != null && r.SharedMailbox.SharedMailboxCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
            }

            if (scopedCompanies is { Count: > 0 })
            {
                var accessibleOnboardingRequestIds = CompanyScopeHelper
                    .ApplyOnboardingCompanyScope(context.OnboardingRequests.AsNoTracking(), scopedCompanies)
                    .Select(onboarding => onboarding.Id);

                query = query.Where(r =>
                    r.OnboardingRequestId.HasValue &&
                    accessibleOnboardingRequestIds.Contains(r.OnboardingRequestId.Value));
            }

            if (mailboxId.HasValue)
            {
                query = query.Where(r => r.SharedMailboxId == mailboxId.Value);
            }

            var reportQuery = query.ProjectToType<SharedMailboxAccessReportViewModel>();

            if (!string.IsNullOrEmpty(searchString))
            {
                reportQuery = reportQuery.Where(r =>
                    r.EmployeeName.Contains(searchString) ||
                    r.MailboxName.Contains(searchString) ||
                    r.MailboxAddress.Contains(searchString) ||
                    r.Department.Contains(searchString) ||
                    r.Branch.Contains(searchString));
            }

            return reportQuery.OrderByDescending(r => r.DateGranted);
        }
    }
}

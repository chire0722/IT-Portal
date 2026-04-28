// Project comment: EmailGroupReportQueries.cs is part of the IT Form application source code.

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

namespace IT_Form.Application.Features.EmailGroup
{
    /// <summary>
    /// Carries data needed to execute the GetEmailGroupReportPage query.
    /// </summary>
    public sealed record GetEmailGroupReportPageQuery(int? Id, string? SearchString, int Page = 1) : IRequest<ProcessResult<EmailGroupReportPageViewModel>>;
    /// <summary>
    /// Carries data needed to execute the ExportEmailGroupReportCsv query.
    /// </summary>
    public sealed record ExportEmailGroupReportCsvQuery(int? Id, string? SearchString) : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the GetEmailGroupReportPageQuery query workflow.
    /// </summary>
    public sealed class GetEmailGroupReportPageQueryHandler : IRequestHandler<GetEmailGroupReportPageQuery, ProcessResult<EmailGroupReportPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetEmailGroupReportPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<EmailGroupReportPageViewModel>> Handle(GetEmailGroupReportPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            string groupName = string.Empty;
            if (request.Id.HasValue)
            {
                var group = await _context.EmailGroups
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        item.GroupName,
                        CompanyIds = item.EmailGroupCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (group == null)
                {
                    return ProcessResult<EmailGroupReportPageViewModel>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !group.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<EmailGroupReportPageViewModel>.Forbidden();
                }

                groupName = group.GroupName;
            }

            var page = request.Page < 1 ? 1 : request.Page;
            const int pageSize = 15;
            var report = await PaginatedList<EmailGroupAccessReportViewModel>.CreateAsync(
                EmailGroupReportBuilder.BuildReportQuery(_context, request.Id, request.SearchString, scopedCompanies, accessibleCompanyIds),
                page,
                pageSize);

            return ProcessResult<EmailGroupReportPageViewModel>.Success(new EmailGroupReportPageViewModel
            {
                Report = report,
                CurrentFilter = request.SearchString,
                GroupId = request.Id,
                GroupName = groupName
            });
        }
    }

    /// <summary>
    /// Handles the ExportEmailGroupReportCsvQuery query workflow.
    /// </summary>
    public sealed class ExportEmailGroupReportCsvQueryHandler : IRequestHandler<ExportEmailGroupReportCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportEmailGroupReportCsvQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportEmailGroupReportCsvQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            if (request.Id.HasValue)
            {
                var group = await _context.EmailGroups
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        CompanyIds = item.EmailGroupCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (group == null)
                {
                    return ProcessResult<BinaryFilePayload>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !group.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<BinaryFilePayload>.Forbidden();
                }
            }

            var reportData = await EmailGroupReportBuilder.BuildReportQuery(_context, request.Id, request.SearchString, scopedCompanies, accessibleCompanyIds)
                .ToListAsync(cancellationToken);

            var builder = new StringBuilder();
            builder.AppendLine("Employee Name,Department,Branch,Group Name,Group Email,Request Type,Request ID,Date Granted");

            foreach (var item in reportData)
            {
                var empName = item.EmployeeName?.Replace("\"", "\"\"") ?? string.Empty;
                var dept = item.Department?.Replace("\"", "\"\"") ?? string.Empty;
                var branch = item.Branch?.Replace("\"", "\"\"") ?? string.Empty;
                var gName = item.GroupName?.Replace("\"", "\"\"") ?? string.Empty;
                var gEmail = item.GroupEmail?.Replace("\"", "\"\"") ?? string.Empty;
                var reqType = item.RequestType?.Replace("\"", "\"\"") ?? string.Empty;
                var dateGranted = item.DateGranted?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

                builder.AppendLine($"\"{empName}\",\"{dept}\",\"{branch}\",\"{gName}\",\"{gEmail}\",\"{reqType}\",\"{item.RequestId}\",\"{dateGranted}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var resultData = new byte[bom.Length + bytes.Length];
            Buffer.BlockCopy(bom, 0, resultData, 0, bom.Length);
            Buffer.BlockCopy(bytes, 0, resultData, bom.Length, bytes.Length);

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                resultData,
                "text/csv",
                $"email_group_access_report_{DateTime.Now:yyyyMMdd}.csv"));
        }
    }

    /// <summary>
    /// Defines the EmailGroupReportBuilder class used by the IT Form application.
    /// </summary>
    internal static class EmailGroupReportBuilder
    {
        /// <summary>
        /// Builds the value returned by the BuildReportQuery operation.
        /// </summary>
        public static IQueryable<EmailGroupAccessReportViewModel> BuildReportQuery(
            ApplicationDbContext context,
            int? groupId,
            string? searchString,
            IReadOnlyCollection<string>? scopedCompanies = null,
            IReadOnlyCollection<int>? accessibleCompanyIds = null)
        {
            var query = context.RequestEmailGroups
                .AsNoTracking()
                .Where(r => r.OnboardingRequest != null && r.OnboardingRequest.Status == RequestStatus.Completed);

            if (accessibleCompanyIds is { Count: > 0 })
            {
                query = query.Where(r => r.EmailGroup != null && r.EmailGroup.EmailGroupCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId)));
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

            if (groupId.HasValue)
            {
                query = query.Where(r => r.EmailGroupId == groupId.Value);
            }

            var reportQuery = query.ProjectToType<EmailGroupAccessReportViewModel>();

            if (!string.IsNullOrEmpty(searchString))
            {
                reportQuery = reportQuery.Where(r =>
                    r.EmployeeName.Contains(searchString) ||
                    r.GroupName.Contains(searchString) ||
                    r.GroupEmail.Contains(searchString) ||
                    r.Department.Contains(searchString) ||
                    r.Branch.Contains(searchString));
            }

            return reportQuery.OrderByDescending(r => r.DateGranted);
        }
    }
}

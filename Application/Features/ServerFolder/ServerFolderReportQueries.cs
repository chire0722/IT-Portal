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

namespace IT_Form.Application.Features.ServerFolder
{
    public sealed record GetServerFolderReportPageQuery(int? Id, string? SearchString, int Page = 1) : IRequest<ProcessResult<ServerFolderReportPageViewModel>>;
    public sealed record ExportServerFolderReportCsvQuery(int? Id, string? SearchString) : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class GetServerFolderReportPageQueryHandler : IRequestHandler<GetServerFolderReportPageQuery, ProcessResult<ServerFolderReportPageViewModel>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public GetServerFolderReportPageQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<ServerFolderReportPageViewModel>> Handle(GetServerFolderReportPageQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            string folderName = string.Empty;
            if (request.Id.HasValue)
            {
                var folder = await _context.ServerFolders
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        item.FolderName,
                        CompanyIds = item.ServerFolderCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (folder == null)
                {
                    return ProcessResult<ServerFolderReportPageViewModel>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !folder.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<ServerFolderReportPageViewModel>.Forbidden();
                }

                folderName = folder.FolderName;
            }

            var page = request.Page < 1 ? 1 : request.Page;
            const int pageSize = 15;
            var report = await PaginatedList<ServerFolderAccessReportViewModel>.CreateAsync(
                ServerFolderReportBuilder.BuildReportQuery(_context, folderName, request.SearchString, scopedCompanies, accessibleCompanyIds),
                page,
                pageSize);

            return ProcessResult<ServerFolderReportPageViewModel>.Success(new ServerFolderReportPageViewModel
            {
                Report = report,
                CurrentFilter = request.SearchString,
                FolderId = request.Id,
                FolderName = folderName
            });
        }
    }

    public sealed class ExportServerFolderReportCsvQueryHandler : IRequestHandler<ExportServerFolderReportCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportServerFolderReportCsvQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportServerFolderReportCsvQuery request, CancellationToken cancellationToken)
        {
            var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
            var accessibleCompanyIds = scopedCompanies.Count > 0
                ? await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken)
                : null;

            string folderName = string.Empty;
            if (request.Id.HasValue)
            {
                var folder = await _context.ServerFolders
                    .AsNoTracking()
                    .Where(item => item.Id == request.Id.Value)
                    .Select(item => new
                    {
                        item.FolderName,
                        CompanyIds = item.ServerFolderCompanies.Select(link => link.CompanyId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (folder == null)
                {
                    return ProcessResult<BinaryFilePayload>.NotFound();
                }

                if (accessibleCompanyIds is { Count: > 0 } && !folder.CompanyIds.All(accessibleCompanyIds.Contains))
                {
                    return ProcessResult<BinaryFilePayload>.Forbidden();
                }

                folderName = folder.FolderName;
            }

            var reportData = await ServerFolderReportBuilder.BuildReportQuery(_context, folderName, request.SearchString, scopedCompanies, accessibleCompanyIds)
                .ToListAsync(cancellationToken);
            var builder = new StringBuilder();
            builder.AppendLine("Employee Name,Department,Branch,Folder Name,Access Level,Request Type,Request ID,Date Granted");

            foreach (var item in reportData)
            {
                var empName = item.EmployeeName?.Replace("\"", "\"\"") ?? string.Empty;
                var dept = item.Department?.Replace("\"", "\"\"") ?? string.Empty;
                var branch = item.Branch?.Replace("\"", "\"\"") ?? string.Empty;
                var folder = item.FolderName?.Replace("\"", "\"\"") ?? string.Empty;
                var access = item.AccessLevel?.Replace("\"", "\"\"") ?? string.Empty;
                var reqType = item.RequestType?.Replace("\"", "\"\"") ?? string.Empty;
                var dateGranted = item.DateGranted?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

                builder.AppendLine($"\"{empName}\",\"{dept}\",\"{branch}\",\"{folder}\",\"{access}\",\"{reqType}\",\"{item.RequestId}\",\"{dateGranted}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var resultData = new byte[bom.Length + bytes.Length];
            Buffer.BlockCopy(bom, 0, resultData, 0, bom.Length);
            Buffer.BlockCopy(bytes, 0, resultData, bom.Length, bytes.Length);

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                resultData,
                "text/csv",
                $"server_folder_access_report_{DateTime.Now:yyyyMMdd}.csv"));
        }
    }

    internal static class ServerFolderReportBuilder
    {
        public static IQueryable<ServerFolderAccessReportViewModel> BuildReportQuery(
            ApplicationDbContext context,
            string? folderName,
            string? searchString,
            IReadOnlyCollection<string>? scopedCompanies = null,
            IReadOnlyCollection<int>? accessibleCompanyIds = null)
        {
            var query = context.FolderPermissions
                .AsNoTracking()
                .Where(f => f.CanRead || f.CanModify)
                .Where(f => f.OnboardingRequest != null && f.OnboardingRequest.Status == RequestStatus.Completed);

            if (accessibleCompanyIds is { Count: > 0 })
            {
                query = query.Where(permission => context.ServerFolders.Any(folder =>
                    folder.FolderName == permission.FolderName &&
                    folder.ServerFolderCompanies.Any(link => accessibleCompanyIds.Contains(link.CompanyId))));
            }

            if (scopedCompanies is { Count: > 0 })
            {
                var accessibleOnboardingRequestIds = CompanyScopeHelper
                    .ApplyOnboardingCompanyScope(context.OnboardingRequests.AsNoTracking(), scopedCompanies)
                    .Select(onboarding => onboarding.Id);

                query = query.Where(permission =>
                    permission.OnboardingRequestId.HasValue &&
                    accessibleOnboardingRequestIds.Contains(permission.OnboardingRequestId.Value));
            }

            if (!string.IsNullOrWhiteSpace(folderName))
            {
                query = query.Where(f => f.FolderName == folderName);
            }

            var reportQuery = query.ProjectToType<ServerFolderAccessReportViewModel>();

            if (!string.IsNullOrEmpty(searchString))
            {
                reportQuery = reportQuery.Where(r =>
                    r.EmployeeName.Contains(searchString) ||
                    r.FolderName.Contains(searchString) ||
                    r.Department.Contains(searchString) ||
                    r.Branch.Contains(searchString));
            }

            return reportQuery.OrderByDescending(r => r.DateGranted);
        }
    }
}

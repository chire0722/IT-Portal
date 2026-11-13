// Project comment: ExportOffboardingCsvQueryHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace IT_Form.Application.Features.Offboarding.ExportOffboardingCsv
{
    /// <summary>
    /// Handles the ExportOffboardingCsvQuery query workflow.
    /// </summary>
    public sealed class ExportOffboardingCsvQueryHandler : IRequestHandler<ExportOffboardingCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportOffboardingCsvQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportOffboardingCsvQuery request, CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (user?.HasClaim("UserTitle", "HR") != true)
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            var query = _context.OffboardingRequests.AsNoTracking().AsQueryable();
            var scopedCompanies = HrRecordReadCompanyScopeSupport.GetScopedCompanies(user);

            if (scopedCompanies.Count > 0)
            {
                query = CompanyScopeHelper.ApplyCompanyScope(query, scopedCompanies, item => item.Branch);
            }

            query = ApplySearchFilter(query, request.SearchString, request.Status);
            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

            var builder = new StringBuilder();
            builder.AppendLine("ID,Full Name,Employee ID,Department,Company,Status,Last Day,Manager Email,HR Signed,Manager Signed,IT Signed,Employee Signed");
            foreach (var r in list)
            {
                builder.AppendLine($"\"{CsvExportSanitizer.EscapeCell(r.Id.ToString())}\",\"{CsvExportSanitizer.EscapeCell(r.FullName)}\",\"{CsvExportSanitizer.EscapeCell(r.EmployeeID)}\",\"{CsvExportSanitizer.EscapeCell(r.Department)}\",\"{CsvExportSanitizer.EscapeCell(r.Branch)}\",\"{r.Status}\",\"{r.LastDayOfWork:yyyy-MM-dd}\",\"{CsvExportSanitizer.EscapeCell(r.HODEmail)}\",\"{r.HRSignedAt:yyyy-MM-dd}\",\"{r.HODSignedAt:yyyy-MM-dd}\",\"{r.ITAdminSignedAt:yyyy-MM-dd}\",\"{r.EmployeeSignedAt:yyyy-MM-dd}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var resultData = new byte[bom.Length + bytes.Length];
            Buffer.BlockCopy(bom, 0, resultData, 0, bom.Length);
            Buffer.BlockCopy(bytes, 0, resultData, bom.Length, bytes.Length);
            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                resultData,
                "text/csv",
                $"offboarding_export_{DateTime.Now:yyyyMMdd}.csv"));
        }

        /// <summary>
        /// Applies changes for the ApplySearchFilter operation.
        /// </summary>
        private static IQueryable<OffboardingRequest> ApplySearchFilter(IQueryable<OffboardingRequest> query, string? searchString, RequestStatus? status)
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r =>
                    (r.FullName != null && r.FullName.Contains(searchString)) ||
                    (r.EmployeeID != null && r.EmployeeID.Contains(searchString)) ||
                    (r.Department != null && r.Department.Contains(searchString)));
            }

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return query;
        }

    }
}

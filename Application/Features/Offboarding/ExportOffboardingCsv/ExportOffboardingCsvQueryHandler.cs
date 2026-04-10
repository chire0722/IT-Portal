using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace IT_Form.Application.Features.Offboarding.ExportOffboardingCsv
{
    public sealed class ExportOffboardingCsvQueryHandler : IRequestHandler<ExportOffboardingCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportOffboardingCsvQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(ExportOffboardingCsvQuery request, CancellationToken cancellationToken)
        {
            var query = _context.OffboardingRequests.AsNoTracking().AsQueryable();
            var user = _currentUserAccessor.User;

            if (!user.HasClaim("UserTitle", "IT") && !user.HasClaim("UserTitle", "HR"))
            {
                var userCompanies = UserHelper.GetClaimValues(user, "UserCompany");
                if (userCompanies.Any())
                {
                    var currentUserName = _currentUserAccessor.UserName;
                    query = query.Where(r =>
                        (r.Branch != null && userCompanies.Any(c =>
                            c == r.Branch ||
                            c.StartsWith(r.Branch + " (") ||
                            r.Branch.StartsWith(c + " (")))
                        || r.CreatedByUserId == currentUserName);
                }
            }

            query = ApplySearchFilter(query, request.SearchString, request.Status);
            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

            var builder = new StringBuilder();
            builder.AppendLine("ID,Full Name,Employee ID,Department,Company,Status,Last Day,Manager Email,HR Signed,Manager Signed,IT Signed,Employee Signed");
            foreach (var r in list)
            {
                builder.AppendLine($"\"{Escape(r.Id.ToString())}\",\"{Escape(r.FullName)}\",\"{Escape(r.EmployeeID)}\",\"{Escape(r.Department)}\",\"{Escape(r.Branch)}\",\"{r.Status}\",\"{r.LastDayOfWork:yyyy-MM-dd}\",\"{Escape(r.HODEmail)}\",\"{r.HRSignedAt:yyyy-MM-dd}\",\"{r.HODSignedAt:yyyy-MM-dd}\",\"{r.ITAdminSignedAt:yyyy-MM-dd}\",\"{r.EmployeeSignedAt:yyyy-MM-dd}\"");
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

        private static string Escape(string? value) => value?.Replace("\"", "\"\"") ?? string.Empty;
    }
}

// Project comment: ExportOnboardingCsvQueryHandler.cs is part of the IT Form application source code.

using System.Text;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.ExportOnboardingCsv
{
    /// <summary>
    /// Handles the ExportOnboardingCsvQuery query workflow.
    /// </summary>
    public sealed class ExportOnboardingCsvQueryHandler
        : IRequestHandler<ExportOnboardingCsvQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public ExportOnboardingCsvQueryHandler(
            ApplicationDbContext context,
            ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(
            ExportOnboardingCsvQuery request,
            CancellationToken cancellationToken)
        {
            var user = _currentUserAccessor.User;
            if (user?.HasClaim("UserTitle", "HR") != true)
            {
                return ProcessResult<BinaryFilePayload>.Forbidden();
            }

            var requests = _context.OnboardingRequests
                .AsNoTracking()
                .AsQueryable()
                .ApplySearchFilter(request.SearchString, request.Status);

            var list = await requests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var builder = new StringBuilder();
            builder.AppendLine("ID,Full Name,Employee ID,Designation,Department,Company,Status,Request Date,Manager Email,HR Signed,IT Signed,Employee Signed");

            foreach (var item in list)
            {
                builder.AppendLine(
                    $"\"{item.Id}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.FullName)}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.EmployeeID)}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.Designation)}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.Department)}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.Branch)}\"," +
                    $"\"{item.Status}\"," +
                    $"\"{item.RequestDate:yyyy-MM-dd}\"," +
                    $"\"{CsvExportSanitizer.EscapeCell(item.HODEmail)}\"," +
                    $"\"{item.RequesterSignedAt:yyyy-MM-dd}\"," +
                    $"\"{item.ITAdminSignedAt:yyyy-MM-dd}\"," +
                    $"\"{item.EmployeeSignedAt:yyyy-MM-dd}\"");
            }

            var payload = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var content = new byte[bom.Length + payload.Length];
            Buffer.BlockCopy(bom, 0, content, 0, bom.Length);
            Buffer.BlockCopy(payload, 0, content, bom.Length, payload.Length);

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                content,
                "text/csv",
                $"onboarding_export_{DateTime.Now:yyyyMMdd}.csv"));
        }


    }
}


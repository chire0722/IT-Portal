using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailbox
{
    public sealed record DownloadSharedMailboxTemplateQuery() : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class DownloadSharedMailboxTemplateQueryHandler : IRequestHandler<DownloadSharedMailboxTemplateQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DownloadSharedMailboxTemplateQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(DownloadSharedMailboxTemplateQuery request, CancellationToken cancellationToken)
        {
            var activeCompanies = await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("SharedMailboxes");
            worksheet.Cell(1, 1).Value = "MailboxName";
            worksheet.Cell(1, 2).Value = "MailboxAddress";
            worksheet.Cell(1, 3).Value = "Company (Dropdown)";
            worksheet.Cell(1, 4).Value = "IsActive";

            var headerRange = worksheet.Row(1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            worksheet.Cell(2, 1).Value = "Sales Mailbox";
            worksheet.Cell(2, 2).Value = "sales@example.com";
            worksheet.Cell(2, 3).Value = activeCompanies.FirstOrDefault() ?? string.Empty;
            worksheet.Cell(2, 4).Value = "TRUE";

            var refSheet = ExcelTemplateDropdownHelper.CreateReferenceSheet(workbook);
            if (ExcelTemplateDropdownHelper.AddNamedList(
                workbook,
                refSheet,
                ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                1,
                "Active Company Names",
                activeCompanies))
            {
                ExcelTemplateDropdownHelper.ApplyDropdownFromNamedList(
                    worksheet,
                    "C2:C1000",
                    ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                    "Select Company",
                    "Please select a company from the dropdown list.",
                    "Invalid Company",
                    "Please select a company from the dropdown list.");
            }

            ExcelTemplateDropdownHelper.ApplyTrueFalseDropdown(worksheet, "D2:D1000");
            worksheet.SheetView.FreezeRows(1);
            ExcelTemplateDropdownHelper.SetColumnWidths(worksheet, 28, 34, 30, 16);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SharedMailboxes_Template.xlsx"));
        }
    }
}

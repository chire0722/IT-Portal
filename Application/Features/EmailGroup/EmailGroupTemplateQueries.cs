using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailGroup
{
    public sealed record DownloadEmailGroupTemplateQuery() : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class DownloadEmailGroupTemplateQueryHandler : IRequestHandler<DownloadEmailGroupTemplateQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DownloadEmailGroupTemplateQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ProcessResult<BinaryFilePayload>> Handle(DownloadEmailGroupTemplateQuery request, CancellationToken cancellationToken)
        {
            var activeCompanies = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("EmailGroups");
            worksheet.Cell(1, 1).Value = "GroupName";
            worksheet.Cell(1, 2).Value = "GroupEmail";
            worksheet.Cell(1, 3).Value = "Company (Dropdown, comma-separated for multiple)";
            worksheet.Cell(1, 4).Value = "IsActive (TRUE/FALSE)";

            var headerRange = worksheet.Range("A1:D1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a9c56");
            headerRange.Style.Font.FontColor = XLColor.White;

            worksheet.Cell(2, 1).Value = "All Staff";
            worksheet.Cell(2, 2).Value = "allstaff@example.com";
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
                    "Select a company from the dropdown. For multiple companies, separate valid names with commas.",
                    "Invalid Company",
                    "Use the dropdown values or enter comma-separated company names exactly as listed on the Reference sheet.");
            }

            ExcelTemplateDropdownHelper.ApplyTrueFalseDropdown(worksheet, "D2:D1000");
            refSheet.Cell(1, 2).Value = "Notes";
            refSheet.Cell(2, 2).Value = "For multi-company rows, separate valid company names with commas in the import sheet cell.";
            refSheet.Column(2).Width = 78;
            worksheet.SheetView.FreezeRows(1);
            ExcelTemplateDropdownHelper.SetColumnWidths(worksheet, 24, 34, 42, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EmailGroups_Template.xlsx"));
        }
    }
}

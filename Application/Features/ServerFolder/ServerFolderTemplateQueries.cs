// Project comment: ServerFolderTemplateQueries.cs is part of the IT Form application source code.

using ClosedXML.Excel;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolder
{
    /// <summary>
    /// Carries data needed to execute the DownloadServerFolderTemplate query.
    /// </summary>
    public sealed record DownloadServerFolderTemplateQuery() : IRequest<ProcessResult<BinaryFilePayload>>;

    /// <summary>
    /// Handles the DownloadServerFolderTemplateQuery query workflow.
    /// </summary>
    public sealed class DownloadServerFolderTemplateQueryHandler : IRequestHandler<DownloadServerFolderTemplateQuery, ProcessResult<BinaryFilePayload>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DownloadServerFolderTemplateQueryHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<ProcessResult<BinaryFilePayload>> Handle(DownloadServerFolderTemplateQuery request, CancellationToken cancellationToken)
        {
            var companyDisplayNames = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(
                _context,
                _currentUserAccessor.User,
                cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("ServerFolders");
            worksheet.Cell(1, 1).Value = "FolderName";
            worksheet.Cell(1, 2).Value = "Company (Dropdown, comma-separated for multiple)";
            worksheet.Cell(1, 3).Value = "IsActive (TRUE/FALSE)";

            var headerRange = worksheet.Range("A1:C1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a9c56");
            headerRange.Style.Font.FontColor = XLColor.White;

            worksheet.Cell(2, 1).Value = "Public_Shared";
            worksheet.Cell(2, 2).Value = companyDisplayNames.FirstOrDefault() ?? string.Empty;
            worksheet.Cell(2, 3).Value = "TRUE";

            var refSheet = ExcelTemplateDropdownHelper.CreateReferenceSheet(workbook);
            if (ExcelTemplateDropdownHelper.AddNamedList(
                workbook,
                refSheet,
                ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                1,
                "Active Company Names",
                companyDisplayNames))
            {
                ExcelTemplateDropdownHelper.ApplyDropdownFromNamedList(
                    worksheet,
                    "B2:B1000",
                    ExcelTemplateDropdownHelper.CompanyOptionsDefinedName,
                    "Select Company",
                    "Select a company from the dropdown. For multiple companies, separate valid names with commas.",
                    "Invalid Company",
                    "Use the dropdown values or enter comma-separated company names exactly as listed on the Reference sheet.");
            }

            ExcelTemplateDropdownHelper.ApplyTrueFalseDropdown(worksheet, "C2:C1000");
            refSheet.Cell(1, 2).Value = "Notes";
            refSheet.Cell(2, 2).Value = "For multi-company rows, separate valid company names with commas in the import sheet cell.";
            refSheet.Column(2).Width = 78;
            worksheet.SheetView.FreezeRows(1);
            ExcelTemplateDropdownHelper.SetColumnWidths(worksheet, 28, 42, 18);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ServerFolders_Template.xlsx"));
        }
    }
}

using ClosedXML.Excel;
using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.StandardSoftware
{
    public sealed record DownloadStandardSoftwareTemplateQuery() : IRequest<ProcessResult<BinaryFilePayload>>;

    public sealed class DownloadStandardSoftwareTemplateQueryHandler
        : IRequestHandler<DownloadStandardSoftwareTemplateQuery, ProcessResult<BinaryFilePayload>>
    {
        public Task<ProcessResult<BinaryFilePayload>> Handle(DownloadStandardSoftwareTemplateQuery request, CancellationToken cancellationToken)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("SoftwareList");
            worksheet.Cell(1, 1).Value = "SoftwareName";
            worksheet.Cell(1, 2).Value = "IsMandatory";
            worksheet.Cell(1, 3).Value = "IsActive";
            worksheet.Cell(1, 4).Value = "VersionName";
            worksheet.Cell(1, 5).Value = "LicenseKey";
            worksheet.Cell(1, 6).Value = "LicenseType";
            worksheet.Cell(1, 7).Value = "ExpiryDate";
            worksheet.Cell(1, 8).Value = "Quantity";
            worksheet.Cell(1, 9).Value = "IsFree";

            var headerRange = worksheet.Row(1);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            worksheet.Cell(2, 1).Value = "Microsoft Office 365";
            worksheet.Cell(2, 2).Value = "TRUE";
            worksheet.Cell(2, 3).Value = "TRUE";
            worksheet.Cell(2, 4).Value = "Office 365 E3";
            worksheet.Cell(2, 5).Value = "XXXXX-XXXXX-XXXXX";
            worksheet.Cell(2, 6).Value = "Subscription";
            worksheet.Cell(2, 7).Value = "31/12/2026";
            worksheet.Cell(2, 8).Value = 50;
            worksheet.Cell(2, 9).Value = "FALSE";

            worksheet.Cell(3, 1).Value = "AutoCAD 2024";
            worksheet.Cell(3, 2).Value = "FALSE";
            worksheet.Cell(3, 3).Value = "TRUE";
            worksheet.Cell(3, 4).Value = "AutoCAD 2024 Professional";
            worksheet.Cell(3, 5).Value = "YYYYY-YYYYY-YYYYY";
            worksheet.Cell(3, 6).Value = "Perpetual";
            worksheet.Cell(3, 7).Value = "";
            worksheet.Cell(3, 9).Value = "FALSE";

            worksheet.Cell(4, 1).Value = "Google Chrome";
            worksheet.Cell(4, 2).Value = "TRUE";
            worksheet.Cell(4, 3).Value = "TRUE";
            worksheet.Cell(4, 4).Value = "Latest version";
            worksheet.Cell(4, 6).Value = "Perpetual";
            worksheet.Cell(4, 9).Value = "TRUE";

            var boolRange = worksheet.Range("B2:C1000");
            var boolVal = boolRange.CreateDataValidation();
            boolVal.List("TRUE,FALSE", true);
            boolVal.InputTitle = "Select Option";
            boolVal.InputMessage = "Please select TRUE or FALSE.";

            var freeRange = worksheet.Range("I2:I1000");
            var freeVal = freeRange.CreateDataValidation();
            freeVal.List("TRUE,FALSE", true);

            var typeRange = worksheet.Range("F2:F1000");
            var typeVal = typeRange.CreateDataValidation();
            typeVal.List("Subscription,Perpetual", true);

            var notes = workbook.Worksheets.Add("Notes");
            notes.Cell(1, 1).Value = "SoftwareName: Name of the software (category). Required.";
            notes.Cell(2, 1).Value = "IsMandatory: TRUE = auto-selected for all onboarding.";
            notes.Cell(3, 1).Value = "IsActive: TRUE = visible in onboarding form.";
            notes.Cell(4, 1).Value = "VersionName: Software version/edition name. Optional.";
            notes.Cell(5, 1).Value = "LicenseKey: License key for the version. Optional.";
            notes.Cell(6, 1).Value = "LicenseType: Subscription or Perpetual. Default: Perpetual.";
            notes.Cell(7, 1).Value = "ExpiryDate: Format DD/MM/YYYY. Required for Subscriptions.";
            notes.Cell(8, 1).Value = "IsFree: TRUE = Unlimited seats. Required for free software.";
            notes.Cell(9, 1).Value = "TIP: Multiple rows with the same SoftwareName group under the same software entry.";

            notes.Columns().AdjustToContents();
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return Task.FromResult(ProcessResult<BinaryFilePayload>.Success(new BinaryFilePayload(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "SoftwareList_Template.xlsx")));
        }
    }
}

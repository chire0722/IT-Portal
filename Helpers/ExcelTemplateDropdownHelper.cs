using ClosedXML.Excel;

namespace IT_Form.Helpers;

public static class ExcelTemplateDropdownHelper
{
    public const string CompanyOptionsDefinedName = "CompanyOptions";
    public const string DepartmentOptionsDefinedName = "DepartmentOptions";

    public static IXLWorksheet CreateReferenceSheet(
        XLWorkbook workbook,
        string sheetName = "Reference",
        bool hideSheet = false)
    {
        var worksheet = workbook.Worksheets.Add(sheetName);
        if (hideSheet)
        {
            worksheet.Hide();
        }

        return worksheet;
    }

    public static bool AddNamedList(
        XLWorkbook workbook,
        IXLWorksheet referenceSheet,
        string definedName,
        int columnNumber,
        string header,
        IEnumerable<string> values)
    {
        var normalizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        referenceSheet.Cell(1, columnNumber).Value = header;
        referenceSheet.Cell(1, columnNumber).Style.Font.Bold = true;

        if (normalizedValues.Count == 0)
        {
            referenceSheet.Cell(2, columnNumber).Value = "No active values configured";
            referenceSheet.Cell(2, columnNumber).Style.Font.FontColor = XLColor.Gray;
            referenceSheet.Column(columnNumber).Width = 28;
            return false;
        }

        for (var i = 0; i < normalizedValues.Count; i++)
        {
            referenceSheet.Cell(i + 2, columnNumber).Value = normalizedValues[i];
        }

        workbook.DefinedNames.Add(
            definedName,
            referenceSheet.Range(2, columnNumber, normalizedValues.Count + 1, columnNumber));

        referenceSheet.Column(columnNumber).Width = CalculateTextWidth(header, normalizedValues);
        return true;
    }

    public static void ApplyDropdownFromNamedList(
        IXLWorksheet worksheet,
        string rangeAddress,
        string definedName,
        string inputTitle,
        string inputMessage,
        string errorTitle,
        string errorMessage)
    {
        var validation = worksheet.Range(rangeAddress).CreateDataValidation();
        validation.AllowedValues = XLAllowedValues.List;
        validation.List($"={definedName}", true);
        validation.IgnoreBlanks = true;
        validation.InCellDropdown = true;
        validation.ShowInputMessage = true;
        validation.InputTitle = inputTitle;
        validation.InputMessage = inputMessage;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = XLErrorStyle.Stop;
        validation.ErrorTitle = errorTitle;
        validation.ErrorMessage = errorMessage;
    }

    public static void ApplyTrueFalseDropdown(IXLWorksheet worksheet, string rangeAddress)
    {
        var validation = worksheet.Range(rangeAddress).CreateDataValidation();
        validation.AllowedValues = XLAllowedValues.List;
        validation.List("TRUE,FALSE", true);
        validation.IgnoreBlanks = true;
        validation.InCellDropdown = true;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = XLErrorStyle.Stop;
        validation.ErrorTitle = "Invalid Value";
        validation.ErrorMessage = "Please select TRUE or FALSE from the dropdown list.";
    }

    public static void SetColumnWidths(IXLWorksheet worksheet, params double[] widths)
    {
        for (var i = 0; i < widths.Length; i++)
        {
            worksheet.Column(i + 1).Width = widths[i];
        }
    }

    private static double CalculateTextWidth(string header, IEnumerable<string> values)
    {
        var maxLength = values
            .Append(header)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().Length)
            .DefaultIfEmpty(18)
            .Max();

        return Math.Clamp(maxLength + 4, 18, 60);
    }
}

// Project comment: ExcelImportDateHelper.cs is part of the IT Form application source code.

using System.Globalization;
using ClosedXML.Excel;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the ExcelImportDateHelper class used by the IT Form application.
    /// </summary>
    public static class ExcelImportDateHelper
    {
        public const string PreferredDateFormat = "dd/MM/yyyy";
        private static readonly string[] AcceptedDateFormats =
        {
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "yyyy-MM-dd",
            "M/d/yyyy",
            "MM/dd/yyyy"
        };

        /// <summary>
        /// Runs the TryReadOptionalDate operation.
        /// </summary>
        public static bool TryReadOptionalDate(IXLCell cell, out DateTime? value, out string? errorMessage)
        {
            value = null;
            errorMessage = null;

            if (cell == null || cell.IsEmpty())
            {
                return true;
            }

            if (cell.TryGetValue<DateTime>(out var excelDate))
            {
                value = excelDate.Date;
                return true;
            }

            var rawValue = cell.GetString().Trim();
            if (string.IsNullOrEmpty(rawValue))
            {
                return true;
            }

            if (DateTime.TryParseExact(
                    rawValue,
                    AcceptedDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate))
            {
                value = parsedDate.Date;
                return true;
            }

            errorMessage = $"Invalid date '{rawValue}'. Use {PreferredDateFormat}.";
            return false;
        }
    }
}

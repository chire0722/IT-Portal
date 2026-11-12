namespace IT_Form.Helpers;

/// <summary>
/// Neutralizes spreadsheet formula-like prefixes and escapes embedded double quotes.
/// This method does not add the outer CSV quotes; each CSV writer must quote the returned
/// value as one complete field.
/// </summary>
public static class CsvExportSanitizer
{
    public static string EscapeCell(string? value)
    {
        var safeValue = value ?? string.Empty;
        var firstMeaningfulCharacter = safeValue.FirstOrDefault(character =>
            !char.IsWhiteSpace(character) && character != '\uFEFF');

        // Prefix an apostrophe so spreadsheet applications treat formula-like user input as text.
        if (firstMeaningfulCharacter is '=' or '+' or '-' or '@'
            || safeValue.StartsWith('\t')
            || safeValue.StartsWith('\r')
            || safeValue.StartsWith('\n'))
        {
            safeValue = "'" + safeValue;
        }

        return safeValue.Replace("\"", "\"\"", StringComparison.Ordinal);
    }
}

namespace IT_Form.Helpers;

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

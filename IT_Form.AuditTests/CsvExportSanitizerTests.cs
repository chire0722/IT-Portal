using IT_Form.Helpers;

namespace IT_Form.AuditTests;

public sealed class CsvExportSanitizerTests
{
    [Theory]
    [InlineData("=HYPERLINK(\"https://example.invalid\")", "'=HYPERLINK(\"\"https://example.invalid\"\")")]
    [InlineData("  +SUM(1,2)", "'  +SUM(1,2)")]
    [InlineData("\t@cmd", "'\t@cmd")]
    [InlineData("-1+1", "'-1+1")]
    public void EscapeCell_PrefixesSpreadsheetFormulaLikeValues(string input, string expected)
    {
        Assert.Equal(expected, CsvExportSanitizer.EscapeCell(input));
    }

    [Fact]
    public void EscapeCell_QuotesEmbeddedDoubleQuotes()
    {
        Assert.Equal("Employee \"\"Test\"\"", CsvExportSanitizer.EscapeCell("Employee \"Test\""));
    }

    [Fact]
    public void EscapeCell_LeavesOrdinaryTextUnchanged()
    {
        Assert.Equal("Tan Beng Hong", CsvExportSanitizer.EscapeCell("Tan Beng Hong"));
    }
}

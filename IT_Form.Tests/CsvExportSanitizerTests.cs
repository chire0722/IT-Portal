using IT_Form.Helpers;

namespace IT_Form.Tests;

/// <summary>
/// Verifies spreadsheet formula neutralization and CSV cell escaping.
/// </summary>
public sealed class CsvExportSanitizerTests
{
    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+SUM(1,2)", "'+SUM(1,2)")]
    [InlineData("-1+1", "'-1+1")]
    [InlineData("@SUM(A1:A2)", "'@SUM(A1:A2)")]
    [InlineData("\t=1+1", "'\t=1+1")]
    [InlineData("\r=1+1", "'\r=1+1")]
    [InlineData("\n=1+1", "'\n=1+1")]
    [InlineData(" \t=1+1", "' \t=1+1")]
    [InlineData("\uFEFF=1+1", "'\uFEFF=1+1")]
    public void EscapeCell_PrefixesFormulaLikeValuesAfterLeadingWhitespace(
        string input,
        string expected)
    {
        Assert.Equal(expected, CsvExportSanitizer.EscapeCell(input));
    }

    [Fact]
    public void EscapeCell_DoublesQuotesButLeavesOuterCsvQuotingToCaller()
    {
        const string input = "Employee, \"A\"\r\nSecond line";

        var escaped = CsvExportSanitizer.EscapeCell(input);
        var csvCell = $"\\\"{escaped}\\\"";

        Assert.Equal("Employee, \"\"A\"\"\r\nSecond line", escaped);
        Assert.Equal("\\\"Employee, \"\"A\"\"\r\nSecond line\\\"", csvCell);
    }
}

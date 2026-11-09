using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Tests;

public sealed class FileUploadHelperTests
{
    [Fact]
    public void IsValidExcelFile_AcceptsXlsxWithZipSignature()
    {
        using var stream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4 });
        var file = new FormFile(stream, 0, stream.Length, "file", "workbook.xlsx")
        {
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        Assert.True(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsExecutableRenamedAsXlsx()
    {
        using var stream = new MemoryStream(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4 });
        var file = new FormFile(stream, 0, stream.Length, "file", "payload.xlsx")
        {
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsUnsupportedMimeType()
    {
        using var stream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x03, 0x04, 1, 2, 3, 4 });
        var file = new FormFile(stream, 0, stream.Length, "file", "workbook.xlsx")
        {
            ContentType = "text/html"
        };

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_AcceptsLegacyXlsWithOleSignature()
    {
        using var stream = new MemoryStream(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
        var file = new FormFile(stream, 0, stream.Length, "file", "legacy.xls")
        {
            ContentType = "application/vnd.ms-excel"
        };

        Assert.True(FileUploadHelper.IsValidExcelFile(file, out _));
    }
}

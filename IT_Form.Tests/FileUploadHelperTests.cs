using System.IO.Compression;
using System.Text;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Tests;

public sealed class FileUploadHelperTests
{
    [Fact]
    public void IsValidExcelFile_AcceptsXlsxWithValidPackageStructure()
    {
        using var stream = CreateXlsxPackage();
        var file = CreateFormFile(stream, "workbook.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Assert.True(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsExecutableRenamedAsXlsx()
    {
        using var stream = new MemoryStream(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4 });
        var file = CreateFormFile(stream, "payload.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsUnsupportedMimeType()
    {
        using var stream = CreateXlsxPackage();
        var file = CreateFormFile(stream, "workbook.xlsx", "text/html");

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsLegacyXlsBecauseImporterUsesClosedXml()
    {
        using var stream = new MemoryStream(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });
        var file = CreateFormFile(stream, "legacy.xls", "application/vnd.ms-excel");

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    [Fact]
    public void IsValidExcelFile_RejectsHighCompressionRatioWorkbook()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", "<Types />");
            AddEntry(archive, "xl/workbook.xml", "<workbook />");
            AddEntry(archive, "xl/worksheets/sheet1.xml", new string('0', 2 * 1024 * 1024));
        }
        stream.Position = 0;
        var file = CreateFormFile(stream, "compressed.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Assert.False(FileUploadHelper.IsValidExcelFile(file, out _));
    }

    private static MemoryStream CreateXlsxPackage()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"></Types>");
            AddEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"></workbook>");
            AddEntry(archive, "xl/worksheets/sheet1.xml", "<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"></worksheet>");
        }
        stream.Position = 0;
        return stream;
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static FormFile CreateFormFile(Stream stream, string fileName, string contentType)
        => new(stream, 0, stream.Length, "file", fileName) { ContentType = contentType };
}

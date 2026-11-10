using System.IO;
using System.IO.Compression;
using System.Text;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IT_Form.Tests
{
    public class FileUploadHelperTests
    {
        private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        [Fact]
        public void IsValidExcelFile_AcceptsValidXlsxPackage()
        {
            using var file = CreateFormFile(CreateWorkbook(), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.True(result);
            Assert.Empty(error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsTextRenamedAsXlsx()
        {
            using var file = CreateFormFile(Encoding.UTF8.GetBytes("not an excel workbook"), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void IsValidExcelFile_RejectsLegacyXlsBecauseImporterRequiresXlsx()
        {
            using var file = CreateFormFile(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }, "assets.xls", "application/vnd.ms-excel");

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains(".xlsx", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsMismatchedDeclaredMimeType()
        {
            using var file = CreateFormFile(CreateWorkbook(), "assets.xlsx", "text/html");

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("content type", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsMacroEnabledPackage()
        {
            using var file = CreateFormFile(CreateWorkbook(includeMacro: true), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("Macro-enabled", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsZipThatIsNotAnOfficeWorkbook()
        {
            using var file = CreateFormFile(CreateZipWithOnlyEntry("readme.txt", "hello"), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("valid .xlsx", error);
        }

        private static FormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
        {
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] CreateWorkbook(bool includeMacro = false)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"></Types>");
                AddEntry(archive, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"></workbook>");
                if (includeMacro)
                {
                    AddEntry(archive, "xl/vbaProject.bin", "macro");
                }
            }

            return stream.ToArray();
        }

        private static byte[] CreateZipWithOnlyEntry(string name, string value)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                AddEntry(archive, name, value);
            }

            return stream.ToArray();
        }

        private static void AddEntry(ZipArchive archive, string name, string value)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(value);
        }
    }
}

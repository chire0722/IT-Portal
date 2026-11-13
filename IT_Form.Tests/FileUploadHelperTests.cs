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
            var file = CreateFormFile(CreateWorkbook(), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.True(result);
            Assert.Empty(error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsTextRenamedAsXlsx()
        {
            var file = CreateFormFile(Encoding.UTF8.GetBytes("not an excel workbook"), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        [Fact]
        public void IsValidExcelFile_RejectsLegacyXlsBecauseImporterRequiresXlsx()
        {
            var file = CreateFormFile(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }, "assets.xls", "application/vnd.ms-excel");

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains(".xlsx", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsMismatchedDeclaredMimeType()
        {
            var file = CreateFormFile(CreateWorkbook(), "assets.xlsx", "text/html");

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("content type", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsMacroEnabledPackage()
        {
            var file = CreateFormFile(CreateWorkbook(includeMacro: true), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("Macro-enabled", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsZipThatIsNotAnOfficeWorkbook()
        {
            var file = CreateFormFile(CreateZipWithOnlyEntry("readme.txt", "hello"), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("valid .xlsx", error);
        }

        [Fact]
        public void IsValidExcelFile_RejectsZipPathTraversalEntries()
        {
            var file = CreateFormFile(CreateWorkbook(includeUnsafePath: true), "assets.xlsx", XlsxMimeType);

            var result = FileUploadHelper.IsValidExcelFile(file, out var error);

            Assert.False(result);
            Assert.Contains("unsafe", error);
        }

        [Fact]
        public void IsValidExcelFile_EnforcesEndpointSpecificSizeLimit()
        {
            var stream = new MemoryStream(new byte[] { 0x50 });
            var file = new FormFile(stream, 0, 5 * 1024 * 1024 + 1, "file", "folders.xlsx")
            {
                Headers = new HeaderDictionary(),
                ContentType = XlsxMimeType
            };

            var result = FileUploadHelper.IsValidExcelFile(file, out var error, 5 * 1024 * 1024);

            Assert.False(result);
            Assert.Contains("5MB", error);
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


        [Fact]
        public void UploadFileSignatureValidator_AcceptsPdfWithMatchingMimeAndSignature()
        {
            var file = CreateFormFile(Encoding.ASCII.GetBytes("%PDF-1.7"), "resignation.pdf", "application/pdf");

            Assert.True(UploadFileSignatureValidator.HasAllowedSignature(file, ".pdf"));
        }

        [Fact]
        public void UploadFileSignatureValidator_RejectsTextRenamedAsPdf()
        {
            var file = CreateFormFile(Encoding.UTF8.GetBytes("<html>not a pdf</html>"), "resignation.pdf", "application/pdf");

            Assert.False(UploadFileSignatureValidator.HasAllowedSignature(file, ".pdf"));
        }

        [Fact]
        public void UploadFileSignatureValidator_AcceptsPngWithMatchingMimeAndSignature()
        {
            var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
            var file = CreateFormFile(signature, "damage.png", "image/png");

            Assert.True(UploadFileSignatureValidator.HasAllowedSignature(file, ".png"));
        }

        [Fact]
        public void UploadFileSignatureValidator_RejectsMismatchedMimeType()
        {
            var file = CreateFormFile(Encoding.ASCII.GetBytes("%PDF-1.7"), "resignation.pdf", "text/html");

            Assert.False(UploadFileSignatureValidator.HasAllowedSignature(file, ".pdf"));
        }

        [Fact]
        public void UploadFileSignatureValidator_AcceptsWebmEbmlSignature()
        {
            var file = CreateFormFile(new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 0x00 }, "proof.webm", "video/webm");

            Assert.True(UploadFileSignatureValidator.HasAllowedSignature(file, ".webm"));
        }

        private static byte[] CreateWorkbook(bool includeMacro = false, bool includeUnsafePath = false)
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

                if (includeUnsafePath)
                {
                    AddEntry(archive, "../outside.txt", "unsafe");
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

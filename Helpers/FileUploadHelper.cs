// Project comment: FileUploadHelper.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.IO.Compression;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the FileUploadHelper class used by the IT Form application.
    /// </summary>
    public static class FileUploadHelper
    {
        private static readonly string[] AllowedExtensions = { ".xlsx" };
        private static readonly string[] AllowedMimeTypes = {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Runs the IsValidExcelFile operation.
        /// </summary>
        public static bool IsValidExcelFile(IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                errorMessage = "The file is empty or missing.";
                return false;
            }

            if (file.Length > MaxFileSize)
            {
                errorMessage = "The file size exceeds the 10MB limit.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                errorMessage = "Invalid file extension. Only .xlsx and .xls files are allowed.";
                return false;
            }

            var contentType = file.ContentType ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(contentType) &&
                !AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase) &&
                !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The uploaded file content type is not supported.";
                return false;
            }

            // ClosedXML supports Open XML workbooks (.xlsx), not legacy binary .xls files.
            // Verify both the ZIP signature and the actual workbook package structure.
            Span<byte> header = stackalloc byte[4];
            using (var stream = file.OpenReadStream())
            {
                var bytesRead = stream.Read(header);
                if (bytesRead < 4 ||
                    header[0] != 0x50 || header[1] != 0x4B ||
                    header[2] != 0x03 || header[3] != 0x04)
                {
                    errorMessage = "The file content does not match a supported .xlsx workbook.";
                    return false;
                }
            }

            try
            {
                using var packageStream = new MemoryStream();
                using (var source = file.OpenReadStream())
                {
                    source.CopyTo(packageStream);
                }

                packageStream.Position = 0;
                using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
                if (archive.Entries.Count == 0 || archive.Entries.Count > 1000)
                {
                    errorMessage = "The workbook contains an invalid number of package entries.";
                    return false;
                }

                var names = archive.Entries
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!names.Contains("[Content_Types].xml") || !names.Contains("xl/workbook.xml"))
                {
                    errorMessage = "The uploaded ZIP file is not a valid Excel workbook.";
                    return false;
                }

                long totalUncompressedBytes = 0;
                foreach (var entry in archive.Entries)
                {
                    var normalizedName = entry.FullName.Replace('\\', '/');
                    if (normalizedName.StartsWith("/", StringComparison.Ordinal) ||
                        normalizedName.Split('/').Any(segment => segment == ".."))
                    {
                        errorMessage = "The workbook contains an unsafe package path.";
                        return false;
                    }

                    if (entry.Length > 25L * 1024 * 1024)
                    {
                        errorMessage = "A workbook component exceeds the allowed expanded size.";
                        return false;
                    }

                    totalUncompressedBytes += entry.Length;
                    if (totalUncompressedBytes > 100L * 1024 * 1024)
                    {
                        errorMessage = "The workbook expands beyond the allowed processing size.";
                        return false;
                    }

                    if (entry.Length > 1024 * 1024 &&
                        (entry.CompressedLength == 0 || entry.Length / Math.Max(1, entry.CompressedLength) > 100))
                    {
                        errorMessage = "The workbook has an unsafe compression ratio.";
                        return false;
                    }

                    if (normalizedName.Equals("xl/vbaProject.bin", StringComparison.OrdinalIgnoreCase) ||
                        normalizedName.StartsWith("xl/activeX/", StringComparison.OrdinalIgnoreCase) ||
                        normalizedName.StartsWith("xl/embeddings/", StringComparison.OrdinalIgnoreCase) ||
                        normalizedName.StartsWith("xl/externalLinks/", StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Macro-enabled or externally linked workbooks are not supported.";
                        return false;
                    }
                }
            }
            catch (InvalidDataException)
            {
                errorMessage = "The uploaded file is not a valid Excel workbook.";
                return false;
            }
            catch (IOException)
            {
                errorMessage = "The uploaded workbook could not be read.";
                return false;
            }

            return true;
        }
    }
}

// Project comment: FileUploadHelper.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Validates Excel workbooks accepted by bulk-import endpoints.
    /// </summary>
    public static class FileUploadHelper
    {
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB compressed upload
        private const long MaxExpandedSize = 50 * 1024 * 1024; // 50 MB across ZIP entries
        private const long MaxEntrySize = 25 * 1024 * 1024; // 25 MB per ZIP entry
        private const int MaxEntryCount = 2000;
        private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string OctetStreamMimeType = "application/octet-stream";

        /// <summary>
        /// Validates the extension, declared MIME type, and actual OOXML ZIP package.
        /// The upload is not trusted merely because its client-supplied filename ends in .xlsx.
        /// </summary>
        public static bool IsValidExcelFile(IFormFile file, out string errorMessage, long maxFileSize = MaxFileSize)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length <= 0)
            {
                errorMessage = "The file is empty or missing.";
                return false;
            }

            if (maxFileSize <= 0 || file.Length > maxFileSize)
            {
                var maxSizeMb = Math.Max(1, (int)Math.Ceiling(maxFileSize / (1024d * 1024d)));
                errorMessage = $"The file size exceeds the {maxSizeMb}MB limit.";
                return false;
            }

            var safeClientFileName = (file.FileName ?? string.Empty).Replace('\\', '/');
            var extension = Path.GetExtension(safeClientFileName).ToLowerInvariant();
            if (!string.Equals(extension, ".xlsx", StringComparison.Ordinal))
            {
                errorMessage = "Invalid file type. Only .xlsx workbooks are supported.";
                return false;
            }

            var contentType = (file.ContentType ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(contentType) &&
                !string.Equals(contentType, XlsxMimeType, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(contentType, OctetStreamMimeType, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The uploaded file has an unsupported content type.";
                return false;
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

                if (archive.Entries.Count == 0 || archive.Entries.Count > MaxEntryCount)
                {
                    errorMessage = "The workbook package contains an invalid number of entries.";
                    return false;
                }

                long expandedSize = 0;
                var entryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hasContentTypes = false;
                var hasWorkbook = false;

                foreach (var entry in archive.Entries)
                {
                    var normalizedEntryName = entry.FullName.Replace('\\\\', '/');
                    var entrySegments = normalizedEntryName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
                    var hasUnsafePath = normalizedEntryName.StartsWith('/', StringComparison.Ordinal)
                        || normalizedEntryName.Contains(':', StringComparison.Ordinal)
                        || entrySegments.Any(segment => segment is "." or "..");

                    if (string.IsNullOrWhiteSpace(entry.FullName) ||
                        hasUnsafePath ||
                        unixFileType == 0xA000 ||
                        !entryNames.Add(entry.FullName) ||
                        entry.Length < 0 ||
                        entry.Length > MaxEntrySize)
                    {
                        errorMessage = "The workbook package contains invalid, unsafe, or duplicate entries.";
                        return false;
                    }

                    expandedSize = checked(expandedSize + entry.Length);
                    if (expandedSize > MaxExpandedSize)
                    {
                        errorMessage = "The expanded workbook exceeds the 50MB safety limit.";
                        return false;
                    }

                    if (string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase))
                    {
                        hasContentTypes = true;
                    }
                    else if (string.Equals(entry.FullName, "xl/workbook.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        hasWorkbook = true;
                    }
                    else if (string.Equals(entry.FullName, "xl/vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Macro-enabled workbooks are not accepted.";
                        return false;
                    }
                }

                if (!hasContentTypes || !hasWorkbook)
                {
                    errorMessage = "The uploaded file is not a valid .xlsx workbook.";
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (
                ex is InvalidDataException ||
                ex is IOException ||
                ex is ArgumentException ||
                ex is NotSupportedException ||
                ex is OverflowException)
            {
                errorMessage = "The uploaded file is not a valid .xlsx workbook.";
                return false;
            }
        }
    }
}

// Project comment: FileUploadHelper.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the FileUploadHelper class used by the IT Form application.
    /// </summary>
    public static class FileUploadHelper
    {
        private static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };
        private static readonly string[] AllowedMimeTypes = { 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
            "application/vnd.ms-excel" 
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

            // Check the file signature instead of trusting the user-supplied extension/MIME type.
            // XLSX is a ZIP-based Open XML package; legacy XLS uses the OLE Compound File signature.
            Span<byte> header = stackalloc byte[8];
            using (var stream = file.OpenReadStream())
            {
                var bytesRead = stream.Read(header);
                var isXlsx = extension == ".xlsx" &&
                    bytesRead >= 4 &&
                    header[0] == 0x50 && header[1] == 0x4B &&
                    header[2] == 0x03 && header[3] == 0x04;
                var isXls = extension == ".xls" &&
                    bytesRead == 8 &&
                    header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 });

                if (!isXlsx && !isXls)
                {
                    errorMessage = "The file content does not match a supported Excel file format.";
                    return false;
                }
            }

            return true;
        }
    }
}

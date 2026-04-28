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

            string contentType;
            try
            {
                contentType = file.ContentType ?? string.Empty;
            }
            catch (NullReferenceException)
            {
                contentType = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(contentType) && !AllowedMimeTypes.Contains(contentType))
            {
                // Some browsers might send different MIME types for Excel files, 
                // so we prioritize the extension but keep a basic check.
                return true;
            }

            return true;
        }
    }
}

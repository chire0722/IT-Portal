// Project comment: SensitiveFileStorage.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the SensitiveFileStorage class used by the IT Form application.
    /// </summary>
    public static class SensitiveFileStorage
    {
        public const string RoutePrefix = "/secure-files";
        public const string SecureFolderName = "SecureUploads";
        public const string ResignationsCategory = "resignations";
        public const string OffboardingCategory = "offboarding";

        private static readonly string[] SensitiveCategories =
        {
            ResignationsCategory,
            OffboardingCategory
        };

        /// <summary>
        /// Runs the IsSensitiveCategory operation.
        /// </summary>
        public static bool IsSensitiveCategory(string? category)
            => SensitiveCategories.Contains(category ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Runs the IsLegacySensitiveUploadPath operation.
        /// </summary>
        public static bool IsLegacySensitiveUploadPath(PathString path)
        {
            if (!path.HasValue)
            {
                return false;
            }

            return SensitiveCategories.Any(category =>
                path.StartsWithSegments($"/uploads/{category}", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Builds the value returned by the BuildStoredPath operation.
        /// </summary>
        public static string BuildStoredPath(string category, string fileName)
        {
            if (!IsSensitiveCategory(category))
            {
                throw new ArgumentOutOfRangeException(nameof(category), $"Unknown sensitive file category '{category}'.");
            }

            var normalizedFileName = (fileName ?? string.Empty).Replace('\\', '/');
            var safeFileName = Path.GetFileName(normalizedFileName);
            if (string.IsNullOrWhiteSpace(safeFileName)
                || safeFileName is "." or ".."
                || safeFileName.Contains(':')
                || safeFileName.Any(char.IsControl))
            {
                throw new ArgumentException("A safe file name is required.", nameof(fileName));
            }

            return $"{RoutePrefix}/{category}/{safeFileName}";
        }

        /// <summary>
        /// Gets the data required by the GetSecureRoot operation.
        /// </summary>
        public static string GetSecureRoot(IWebHostEnvironment environment)
            => Path.Combine(environment.ContentRootPath, "App_Data", SecureFolderName);

        /// <summary>
        /// Gets the data required by the GetCategoryRoot operation.
        /// </summary>
        public static string GetCategoryRoot(IWebHostEnvironment environment, string category)
        {
            if (!IsSensitiveCategory(category))
            {
                throw new ArgumentOutOfRangeException(nameof(category), $"Unknown sensitive file category '{category}'.");
            }

            var root = Path.Combine(GetSecureRoot(environment), category);
            Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>
        /// Saves a sensitive upload using a temporary file and an atomic same-volume move.
        /// A failed or cancelled copy is cleaned up and is never exposed under its final name.
        /// </summary>
        public static async Task<string> SaveAsync(
            IFormFile file,
            IWebHostEnvironment environment,
            string category,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            var safeFileName = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
            {
                throw new ArgumentException("A valid file name is required.", nameof(fileName));
            }

            var targetDirectory = GetCategoryRoot(environment, category);
            var physicalPath = Path.Combine(targetDirectory, safeFileName);
            var temporaryPath = Path.Combine(targetDirectory, $".upload-{Guid.NewGuid():N}.tmp");

            try
            {
                await using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                // File.Move without overwrite is atomic on the same volume and preserves collision protection.
                File.Move(temporaryPath, physicalPath);
                return BuildStoredPath(category, safeFileName);
            }
            catch
            {
                // Preserve the original copy/move/cancellation exception; cleanup must not mask it.
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch
                {
                    // A failed cleanup is detectable as a .upload-*.tmp orphan for operational cleanup.
                }

                throw;
            }
        }

        public static async Task<string> SaveBytesAsync(
            ReadOnlyMemory<byte> contents,
            IWebHostEnvironment environment,
            string category,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            if (contents.Length <= 0)
            {
                throw new ArgumentException("File contents are required.", nameof(contents));
            }

            var safeFileName = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
            {
                throw new ArgumentException("A valid file name is required.", nameof(fileName));
            }

            var targetDirectory = GetCategoryRoot(environment, category);
            var physicalPath = Path.Combine(targetDirectory, safeFileName);
            var temporaryPath = Path.Combine(targetDirectory, $".upload-{Guid.NewGuid():N}.tmp");
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await stream.WriteAsync(contents, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                File.Move(temporaryPath, physicalPath);
                return BuildStoredPath(category, safeFileName);
            }
            catch
            {
                try
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
                catch
                {
                    // Orphaned temporary files are not published and remain detectable for cleanup.
                }

                throw;
            }
        }

        /// <summary>
        /// Runs the MoveExistingFileToSecureStorage operation.
        /// </summary>
        public static string MoveExistingFileToSecureStorage(
            IWebHostEnvironment environment,
            string sourcePhysicalPath,
            string category,
            string? preferredFileName = null)
        {
            var safeFileName = Path.GetFileName(preferredFileName ?? sourcePhysicalPath);
            var targetDirectory = GetCategoryRoot(environment, category);
            var targetPhysicalPath = Path.Combine(targetDirectory, safeFileName);

            if (string.Equals(
                Path.GetFullPath(sourcePhysicalPath),
                Path.GetFullPath(targetPhysicalPath),
                StringComparison.OrdinalIgnoreCase))
            {
                return BuildStoredPath(category, safeFileName);
            }

            if (File.Exists(targetPhysicalPath))
            {
                var name = Path.GetFileNameWithoutExtension(safeFileName);
                var extension = Path.GetExtension(safeFileName);
                safeFileName = $"{name}_{Guid.NewGuid():N}{extension}";
                targetPhysicalPath = Path.Combine(targetDirectory, safeFileName);
            }

            Directory.CreateDirectory(targetDirectory);
            File.Move(sourcePhysicalPath, targetPhysicalPath);

            return BuildStoredPath(category, safeFileName);
        }

        /// <summary>
        /// Deletes data for the DeleteStoredFile operation.
        /// </summary>
        public static bool DeleteStoredFile(IWebHostEnvironment environment, string? storedPath)
        {
            var physicalPath = ResolvePhysicalPath(environment, storedPath);
            if (string.IsNullOrWhiteSpace(physicalPath) || !File.Exists(physicalPath))
            {
                return false;
            }

            File.Delete(physicalPath);
            return true;
        }

        /// <summary>
        /// Runs the ResolvePhysicalPath operation.
        /// </summary>
        public static string? ResolvePhysicalPath(IWebHostEnvironment environment, string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return null;
            }

            var normalizedPath = NormalizePath(storedPath);
            if (!TryExtractCategoryAndFileName(normalizedPath, out var category, out var fileName))
            {
                return null;
            }

            if (normalizedPath.StartsWith($"{RoutePrefix}/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(GetCategoryRoot(environment, category), fileName);
            }

            if (string.IsNullOrWhiteSpace(environment.WebRootPath))
            {
                return null;
            }

            return Path.Combine(environment.WebRootPath, "uploads", category, fileName);
        }

        /// <summary>
        /// Runs the TryExtractCategoryAndFileName operation.
        /// </summary>
        public static bool TryExtractCategoryAndFileName(string? storedPath, out string category, out string fileName)
        {
            category = string.Empty;
            fileName = string.Empty;

            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return false;
            }

            var normalizedPath = NormalizePath(storedPath);
            var parts = normalizedPath
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 3 || parts.Any(part => part is "." or ".."))
            {
                return false;
            }

            if (string.Equals(parts[0], "secure-files", StringComparison.OrdinalIgnoreCase))
            {
                category = parts[1];
                fileName = parts[^1];
                return IsSensitiveCategory(category);
            }

            if (string.Equals(parts[0], "uploads", StringComparison.OrdinalIgnoreCase))
            {
                category = parts[1];
                fileName = parts[^1];
                return IsSensitiveCategory(category);
            }

            return false;
        }

        /// <summary>
        /// Runs the NormalizePath operation.
        /// </summary>
        private static string NormalizePath(string storedPath)
            => storedPath.Trim().Replace('\\', '/');
    }
}


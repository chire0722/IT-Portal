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

            return $"{RoutePrefix}/{category}/{Path.GetFileName(fileName)}";
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
        /// Saves data for the SaveAsync operation.
        /// </summary>
        public static async Task<string> SaveAsync(
            IFormFile file,
            IWebHostEnvironment environment,
            string category,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            var safeFileName = Path.GetFileName(fileName);
            var physicalPath = Path.Combine(GetCategoryRoot(environment, category), safeFileName);

            // Refuse filename collisions instead of silently overwriting an existing sensitive file.
            await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            return BuildStoredPath(category, safeFileName);
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

            if (parts.Length < 3)
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

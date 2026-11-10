using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Helpers;

/// <summary>
/// Stores company logos using unique names and atomic same-directory moves.
/// Deletion is restricted to the managed company-logo directory.
/// </summary>
public static class CompanyLogoStorage
{
    private const long MaxLogoBytes = 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    public static async Task<string?> SaveAsync(
        IWebHostEnvironment environment,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (file == null || file.Length <= 0)
        {
            return null;
        }

        if (file.Length > MaxLogoBytes || !UploadFileSignatureValidator.HasAllowedSignature(file, AllowedExtensions))
        {
            throw new ValidationException("The company logo is not a supported image or exceeds the permitted size.");
        }

        var webRoot = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            throw new InvalidOperationException("The web root is not configured.");
        }

        var logoDirectory = Path.GetFullPath(Path.Combine(webRoot, "uploads", "companies", "logos"));
        Directory.CreateDirectory(logoDirectory);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"company-logo-{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(logoDirectory, fileName);
        var temporary = Path.Combine(logoDirectory, $".{Guid.NewGuid():N}.uploading");

        try
        {
            await using (var stream = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await file.CopyToAsync(stream, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, destination, overwrite: false);
            return Path.Combine("uploads", "companies", "logos", fileName).Replace('\\', '/');
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static void DeleteIfManaged(IWebHostEnvironment environment, string? relativePath)
    {
        if (environment == null || string.IsNullOrWhiteSpace(environment.WebRootPath)
            || string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var logoDirectory = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "companies", "logos"));
        var candidate = Path.GetFullPath(Path.Combine(environment.WebRootPath, relativePath.TrimStart('/', '\\')));
        var rootWithSeparator = logoDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? logoDirectory
            : logoDirectory + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (!candidate.StartsWith(rootWithSeparator, comparison))
        {
            return;
        }

        if (File.Exists(candidate))
        {
            File.Delete(candidate);
        }
    }
}

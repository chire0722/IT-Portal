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
                await using var source = file.OpenReadStream();
                var buffer = new byte[64 * 1024];
                long totalBytes = 0;
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    totalBytes += read;
                    if (totalBytes > MaxLogoBytes)
                    {
                        throw new ValidationException("The company logo exceeds the permitted size.");
                    }

                    await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                if (totalBytes == 0)
                {
                    throw new ValidationException("The company logo is empty.");
                }

                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, destination, overwrite: false);
            return Path.Combine("uploads", "companies", "logos", fileName).Replace('\\', '/');
        }
        finally
        {
            try
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
            catch (IOException)
            {
                // Orphaned temporary files are not published and can be removed by deployment cleanup.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original operation result; the private upload directory needs operator cleanup.
            }
        }
    }

    public static string? ResolveManagedPath(IWebHostEnvironment environment, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(environment.WebRootPath) || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            var logoDirectory = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "companies", "logos"));
            var normalizedRelativePath = relativePath.Replace('\\\\', '/').TrimStart('/');
            var candidate = Path.GetFullPath(Path.Combine(environment.WebRootPath, normalizedRelativePath));
            var rootWithSeparator = logoDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? logoDirectory
                : logoDirectory + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var extension = Path.GetExtension(candidate);

            if (!candidate.StartsWith(rootWithSeparator, comparison)
                || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            return candidate;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is NotSupportedException)
        {
            return null;
        }
    }

    public static void DeleteIfManaged(IWebHostEnvironment environment, string? relativePath)
    {
        var candidate = ResolveManagedPath(environment, relativePath);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        try
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
        catch (IOException)
        {
            // A failed cleanup must not roll back a successfully committed database update.
        }
        catch (UnauthorizedAccessException)
        {
            // The deployment identity needs to remediate this orphaned managed file.
        }
    }
}

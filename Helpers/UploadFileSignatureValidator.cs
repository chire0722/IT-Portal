// Project comment: UploadFileSignatureValidator.cs is part of the IT Form application source code.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Helpers;

/// <summary>
/// Validates upload content using both the declared media type and the file signature.
/// Client-supplied filenames and MIME types are not trusted as proof of file content.
/// </summary>
public static class UploadFileSignatureValidator
{
    public static bool HasAllowedSignature(IFormFile? file, params string[] allowedExtensions)
    {
        if (file == null || file.Length <= 0 || allowedExtensions == null || allowedExtensions.Length == 0)
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        string contentType;
        try
        {
            contentType = file.ContentType?.Split(';', 2)[0].Trim() ?? string.Empty;
        }
        catch (NullReferenceException)
        {
            // A malformed/custom IFormFile implementation must fail closed.
            return false;
        }

        if (!HasMatchingMediaType(extension, contentType))
        {
            return false;
        }

        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[12];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = stream.Read(header, bytesRead, header.Length - bytesRead);
                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }

            return HasMatchingSignature(extension, header, bytesRead);
        }
        catch (Exception ex) when (
            ex is IOException ||
            ex is InvalidOperationException ||
            ex is NotSupportedException ||
            ex is ArgumentException)
        {
            return false;
        }
    }

    private static bool HasMatchingMediaType(string extension, string contentType)
        => extension switch
        {
            ".pdf" => string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase),
            ".jpg" or ".jpeg" => string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase),
            ".png" => string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase),
            ".gif" => string.Equals(contentType, "image/gif", StringComparison.OrdinalIgnoreCase),
            ".webp" => string.Equals(contentType, "image/webp", StringComparison.OrdinalIgnoreCase),
            ".mp4" => string.Equals(contentType, "video/mp4", StringComparison.OrdinalIgnoreCase),
            ".mov" => string.Equals(contentType, "video/quicktime", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(contentType, "video/mp4", StringComparison.OrdinalIgnoreCase),
            ".webm" => string.Equals(contentType, "video/webm", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static bool HasMatchingSignature(string extension, byte[] header, int bytesRead)
        => extension switch
        {
            ".pdf" => StartsWith(header, bytesRead, [0x25, 0x50, 0x44, 0x46, 0x2D]), // %PDF-
            ".jpg" or ".jpeg" => StartsWith(header, bytesRead, [0xFF, 0xD8, 0xFF]),
            ".png" => StartsWith(header, bytesRead, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".gif" => StartsWith(header, bytesRead, [0x47, 0x49, 0x46, 0x38]) &&
                      (StartsWith(header, bytesRead, [0x47, 0x49, 0x46, 0x38, 0x37, 0x61]) ||
                       StartsWith(header, bytesRead, [0x47, 0x49, 0x46, 0x38, 0x39, 0x61])),
            ".webp" => bytesRead >= 12 &&
                       StartsWith(header, bytesRead, [0x52, 0x49, 0x46, 0x46]) &&
                       header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            ".mp4" or ".mov" => bytesRead >= 8 &&
                                header[4] == 0x66 && header[5] == 0x74 &&
                                header[6] == 0x79 && header[7] == 0x70, // ftyp
            ".webm" => StartsWith(header, bytesRead, [0x1A, 0x45, 0xDF, 0xA3]),
            _ => false
        };

    private static bool StartsWith(byte[] value, int valueLength, byte[] prefix)
        => valueLength >= prefix.Length && value.AsSpan(0, prefix.Length).SequenceEqual(prefix);
}

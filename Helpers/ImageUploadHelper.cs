using Microsoft.AspNetCore.Http;

namespace IT_Form.Helpers;

/// <summary>
/// Validates company logo uploads using both metadata and file signatures.
/// Only passive raster image formats are accepted; SVG and other active content are excluded.
/// </summary>
public static class ImageUploadHelper
{
    public const long MaxCompanyLogoBytes = 1 * 1024 * 1024;


    public static bool TryDecodeRasterImageDataUrl(string? dataUrl, out string contentType, out byte[] imageBytes)
    {
        contentType = string.Empty;
        imageBytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(dataUrl) || dataUrl.Length > 1_400_100)
        {
            return false;
        }

        var separator = dataUrl.IndexOf(',');
        if (separator <= 0 || separator == dataUrl.Length - 1)
        {
            return false;
        }

        var header = dataUrl[..separator];
        var payload = dataUrl[(separator + 1)..];
        if (!header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        contentType = header[5..^7].Trim().ToLowerInvariant();
        if (contentType is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp"))
        {
            contentType = string.Empty;
            return false;
        }

        try
        {
            imageBytes = Convert.FromBase64String(payload);
        }
        catch (FormatException)
        {
            contentType = string.Empty;
            imageBytes = Array.Empty<byte>();
            return false;
        }

        if (imageBytes.Length == 0 || imageBytes.Length > MaxCompanyLogoBytes)
        {
            contentType = string.Empty;
            imageBytes = Array.Empty<byte>();
            return false;
        }

        var isPng = contentType == "image/png" &&
                    imageBytes.Length >= 8 &&
                    imageBytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isJpeg = contentType == "image/jpeg" &&
                     imageBytes.Length >= 3 &&
                     imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF;
        var isGif = contentType == "image/gif" &&
                    imageBytes.Length >= 6 &&
                    (imageBytes.AsSpan(0, 6).SequenceEqual("GIF87a"u8) || imageBytes.AsSpan(0, 6).SequenceEqual("GIF89a"u8));
        var isWebp = contentType == "image/webp" &&
                     imageBytes.Length >= 12 &&
                     imageBytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     imageBytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

        if (isPng || isJpeg || isGif || isWebp)
        {
            return true;
        }

        contentType = string.Empty;
        imageBytes = Array.Empty<byte>();
        return false;
    }

    public static bool IsValidCompanyLogo(IFormFile? file, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (file == null || file.Length == 0)
        {
            errorMessage = "A non-empty logo image is required.";
            return false;
        }

        if (file.Length > MaxCompanyLogoBytes)
        {
            errorMessage = "Logo must be under 1 MB.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        var contentType = file.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;

        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);

        var isPng = extension == ".png" &&
                    contentType == "image/png" &&
                    read >= 8 &&
                    header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        var isJpeg = (extension == ".jpg" || extension == ".jpeg") &&
                     contentType == "image/jpeg" &&
                     read >= 3 &&
                     header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        var isGif = extension == ".gif" &&
                    contentType == "image/gif" &&
                    read >= 6 &&
                    (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8));

        var isWebp = extension == ".webp" &&
                     contentType == "image/webp" &&
                     read >= 12 &&
                     header[..4].SequenceEqual("RIFF"u8) &&
                     header[8..12].SequenceEqual("WEBP"u8);

        if (isPng || isJpeg || isGif || isWebp)
        {
            return true;
        }

        errorMessage = "Logo must be a valid PNG, JPG, GIF, or WebP image.";
        return false;
    }
}

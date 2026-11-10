// Project comment: ImageReencodingHelper.cs provides deep image parsing and safe re-encoding.

using FluentValidation;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace IT_Form.Helpers;

/// <summary>
/// Fully decodes accepted raster images and writes a fresh, metadata-stripped image.
/// This removes appended payloads and avoids carrying untrusted image metadata into
/// previews, email signatures, logos, or generated documents.
/// </summary>
public static class ImageReencodingHelper
{
    public const int MaxWidth = 4096;
    public const int MaxHeight = 4096;
    public const long MaxPixels = 16_000_000;
    public const long MaxOutputBytes = 4 * 1024 * 1024;

    public static async Task<ReencodedImage> ReencodeAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0)
        {
            throw new ValidationException("The image is empty.");
        }

        try
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, cancellationToken);

            if (image.Width <= 0 || image.Height <= 0 ||
                image.Width > MaxWidth || image.Height > MaxHeight ||
                (long)image.Width * image.Height > MaxPixels)
            {
                throw new ValidationException("The image dimensions exceed the permitted limit.");
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            await using var output = new MemoryStream();
            var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            if (extension is ".jpg" or ".jpeg")
            {
                await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 90 }, cancellationToken);
                extension = ".jpg";
            }
            else
            {
                // PNG output is deterministic and safe for PNG/GIF/WebP previews and logos.
                await image.SaveAsPngAsync(output, new PngEncoder(), cancellationToken);
                extension = ".png";
            }

            if (output.Length <= 0 || output.Length > MaxOutputBytes)
            {
                throw new ValidationException("The re-encoded image exceeds the permitted size.");
            }

            return new ReencodedImage(
                output.ToArray(),
                extension,
                extension == ".jpg" ? "image/jpeg" : "image/png",
                image.Width,
                image.Height);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is UnknownImageFormatException ||
                                  ex is InvalidImageContentException ||
                                  ex is InvalidOperationException ||
                                  ex is IOException ||
                                  ex is ArgumentException)
        {
            throw new ValidationException("The image could not be deeply parsed and re-encoded.");
        }
    }

    public sealed record ReencodedImage(
        byte[] Bytes,
        string Extension,
        string ContentType,
        int Width,
        int Height);
}


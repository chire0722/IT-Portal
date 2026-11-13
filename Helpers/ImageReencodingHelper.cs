// Project comment: ImageReencodingHelper.cs provides deep image parsing and safe re-encoding.

using FluentValidation;
using Microsoft.AspNetCore.Http;
using StbImageSharp;
using StbImageWriteSharp;

namespace IT_Form.Helpers;

/// <summary>
/// Fully decodes accepted raster images and writes a fresh PNG. The decode step
/// rejects malformed/truncated input and the fresh encoding drops source metadata
/// and appended payloads before the image is stored or rendered.
/// </summary>
public static class ImageReencodingHelper
{
    public const int MaxWidth = 4096;
    public const int MaxHeight = 4096;
    public const long MaxPixels = 16_000_000;
    public const long MaxInputBytes = 4 * 1024 * 1024;
    public const long MaxOutputBytes = 4 * 1024 * 1024;

    public static async Task<ReencodedImage> ReencodeAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0 || file.Length > MaxInputBytes)
        {
            throw new ValidationException("The image is empty or exceeds the permitted size.");
        }

        try
        {
            await using var input = file.OpenReadStream();
            using var source = new MemoryStream(capacity: checked((int)file.Length));
            await input.CopyToAsync(source, cancellationToken);
            var bytes = source.ToArray();
            var image = ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha);

            if (image.Width <= 0 || image.Height <= 0 ||
                image.Width > MaxWidth || image.Height > MaxHeight ||
                (long)image.Width * image.Height > MaxPixels)
            {
                throw new ValidationException("The image dimensions exceed the permitted limit.");
            }

            await using var output = new MemoryStream();
            var writer = new ImageWriter();
            writer.WritePng(
                image.Data,
                image.Width,
                image.Height,
                StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha,
                output);

            if (output.Length <= 0 || output.Length > MaxOutputBytes)
            {
                throw new ValidationException("The re-encoded image exceeds the permitted size.");
            }

            return new ReencodedImage(output.ToArray(), ".png", "image/png", image.Width, image.Height);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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


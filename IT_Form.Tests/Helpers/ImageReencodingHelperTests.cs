using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Tests.Helpers;

public sealed class ImageReencodingHelperTests
{
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task ReencodeAsync_DeepParsesAndReencodesPng()
    {
        await using var stream = new MemoryStream(OnePixelPng);
        var file = new FormFile(stream, 0, stream.Length, "image", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await ImageReencodingHelper.ReencodeAsync(file);

        Assert.Equal(".png", result.Extension);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.True(result.Bytes.Length > 8);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], result.Bytes[..8]);
    }

    [Fact]
    public async Task ReencodeAsync_RejectsTruncatedImageThatOnlyHasASignature()
    {
        await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var file = new FormFile(stream, 0, stream.Length, "image", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => ImageReencodingHelper.ReencodeAsync(file));
    }
}


using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Tests;

public sealed class ImageUploadHelperTests
{
    [Fact]
    public void IsValidCompanyLogo_AcceptsPngWithMatchingSignature()
    {
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var file = new FormFile(stream, 0, stream.Length, "logo", "company.png") { ContentType = "image/png" };

        Assert.True(ImageUploadHelper.IsValidCompanyLogo(file, out _));
    }

    [Fact]
    public void IsValidCompanyLogo_RejectsHtmlRenamedAsPng()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<script>alert(1)</script>"));
        var file = new FormFile(stream, 0, stream.Length, "logo", "company.png") { ContentType = "image/png" };

        Assert.False(ImageUploadHelper.IsValidCompanyLogo(file, out _));
    }

    [Fact]
    public void IsValidCompanyLogo_RejectsSvgActiveContent()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"));
        var file = new FormFile(stream, 0, stream.Length, "logo", "company.svg") { ContentType = "image/svg+xml" };

        Assert.False(ImageUploadHelper.IsValidCompanyLogo(file, out _));
    }

    [Fact]
    public void IsValidCompanyLogo_RejectsOversizedImage()
    {
        using var stream = new MemoryStream(new byte[(int)ImageUploadHelper.MaxCompanyLogoBytes + 1]);
        var file = new FormFile(stream, 0, stream.Length, "logo", "company.png") { ContentType = "image/png" };

        Assert.False(ImageUploadHelper.IsValidCompanyLogo(file, out _));
    }

    [Fact]
    public void TryDecodeRasterImageDataUrl_AcceptsPngAndRejectsSvg()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var pngDataUrl = "data:image/png;base64," + Convert.ToBase64String(pngBytes);
        Assert.True(ImageUploadHelper.TryDecodeRasterImageDataUrl(pngDataUrl, out var contentType, out var decoded));
        Assert.Equal("image/png", contentType);
        Assert.Equal(pngBytes, decoded);

        var svgDataUrl = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<svg></svg>"));
        Assert.False(ImageUploadHelper.TryDecodeRasterImageDataUrl(svgDataUrl, out _, out _));
    }

    [Fact]
    public void TryDecodeRasterImageDataUrl_RejectsOversizedPayload()
    {
        var oversized = new byte[ImageUploadHelper.MaxCompanyLogoBytes + 1];
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(oversized);
        Assert.False(ImageUploadHelper.TryDecodeRasterImageDataUrl(dataUrl, out _, out _));
    }
}

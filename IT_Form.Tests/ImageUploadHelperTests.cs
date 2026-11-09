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
}

using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.AuditTests;

public sealed class UploadFileSignatureValidatorTests
{
    [Theory]
    [InlineData("logo.gif", "image/gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })]
    [InlineData("logo.webp", "image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 })]
    [InlineData("logo.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    public void HasAllowedSignature_AcceptsMatchingLogoFormats(string fileName, string contentType, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, stream.Length, "logo", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        Assert.True(UploadFileSignatureValidator.HasAllowedSignature(
            file,
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".webp"));
    }

    [Fact]
    public void HasAllowedSignature_RejectsMismatchedDeclaredType()
    {
        var bytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
        using var stream = new MemoryStream(bytes);
        IFormFile file = new FormFile(stream, 0, stream.Length, "logo", "logo.gif")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        Assert.False(UploadFileSignatureValidator.HasAllowedSignature(file, ".gif"));
    }
}

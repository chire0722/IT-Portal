using System.Buffers.Binary;
using IT_Form.Helpers;

namespace IT_Form.AuditTests;

public sealed class SignatureDataUriValidatorTests
{
    [Fact]
    public void IsValid_AcceptsBoundedPngDataUrl()
    {
        var bytes = CreatePngHeader(800, 600);
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);

        Assert.True(SignatureDataUriValidator.IsValid(dataUrl));
    }

    [Theory]
    [InlineData("data:image/jpeg;base64,AAAA")]
    [InlineData("data:image/png;base64,not-base64")]
    [InlineData("https://example.invalid/signature.png")]
    [InlineData("")]
    public void IsValid_RejectsInvalidOrUnsupportedDataUrls(string dataUrl)
    {
        Assert.False(SignatureDataUriValidator.IsValid(dataUrl));
    }

    [Fact]
    public void IsValid_RejectsOversizedDimensions()
    {
        var bytes = CreatePngHeader(20_000, 20_000);
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);

        Assert.False(SignatureDataUriValidator.IsValid(dataUrl));
    }

    private static byte[] CreatePngHeader(uint width, uint height)
    {
        var bytes = new byte[24];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(8, 4), 13);
        "IHDR"u8.CopyTo(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), width);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), height);
        return bytes;
    }
}

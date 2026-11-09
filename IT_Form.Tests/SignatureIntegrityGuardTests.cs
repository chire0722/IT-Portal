using IT_Form.Services;
using Moq;

namespace IT_Form.Tests;

public sealed class SignatureIntegrityGuardTests
{
    [Fact]
    public void IsValid_ReturnsTrueForCompletelyUnsignedSection()
    {
        var integrity = new Mock<ISignatureIntegrityService>();

        Assert.True(SignatureIntegrityGuard.IsValid(integrity.Object, "OnboardingRequest", "1", null, null, null, null));
        integrity.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsValid_RejectsSignatureWithoutTimestampOrHmac()
    {
        var integrity = new Mock<ISignatureIntegrityService>();

        Assert.False(SignatureIntegrityGuard.IsValid(integrity.Object, "OnboardingRequest", "1", "Signer", null, "data:image/png;base64,AA==", null));
        integrity.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsValid_RejectsTimestampWithoutSignature()
    {
        var integrity = new Mock<ISignatureIntegrityService>();

        Assert.False(SignatureIntegrityGuard.IsValid(integrity.Object, "OnboardingRequest", "1", "Signer", DateTime.UtcNow, null, "v2:ABCD"));
        integrity.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsValid_DelegatesCompleteSignatureToCryptographicVerifier()
    {
        var integrity = new Mock<ISignatureIntegrityService>();
        var signedAt = DateTime.UtcNow;
        integrity.Setup(service => service.Verify("OnboardingRequest", "1", "Signer", signedAt, "signature", "v2:hash"))
            .Returns(true);

        Assert.True(SignatureIntegrityGuard.IsValid(integrity.Object, "OnboardingRequest", "1", "Signer", signedAt, "signature", "v2:hash"));
    }

    [Fact]
    public void IsValid_RejectsLegacyUnversionedHmacWhenVerifierRejectsIt()
    {
        var integrity = new Mock<ISignatureIntegrityService>();
        var signedAt = DateTime.UtcNow;
        integrity.Setup(service => service.Verify("OnboardingRequest", "1", "Signer", signedAt, "signature", "legacy-hmac"))
            .Returns(false);

        Assert.False(SignatureIntegrityGuard.IsValid(integrity.Object, "OnboardingRequest", "1", "Signer", signedAt, "signature", "legacy-hmac"));
    }
}

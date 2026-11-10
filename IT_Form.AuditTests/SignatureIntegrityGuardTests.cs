using IT_Form.Helpers;
using IT_Form.Services;
using Moq;

namespace IT_Form.AuditTests;

public sealed class SignatureIntegrityGuardTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenAllSignatureMetadataIsAbsent()
    {
        var integrity = new Mock<ISignatureIntegrityService>(MockBehavior.Strict);

        var result = SignatureIntegrityGuard.IsValid(
            integrity.Object,
            "OnboardingRequest",
            "42",
            null,
            "Unknown",
            null,
            null,
            null);

        Assert.True(result);
        integrity.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenSignatureMetadataIsPartial()
    {
        var integrity = new Mock<ISignatureIntegrityService>(MockBehavior.Strict);

        var result = SignatureIntegrityGuard.IsValid(
            integrity.Object,
            "OnboardingRequest",
            "42",
            "HR User",
            "Unknown",
            DateTime.UtcNow,
            "base64-signature",
            null);

        Assert.False(result);
        integrity.VerifyNoOtherCalls();
    }

    [Fact]
    public void IsValid_DelegatesToCryptographicVerifier_WhenMetadataIsComplete()
    {
        var signedAt = DateTime.UtcNow;
        var integrity = new Mock<ISignatureIntegrityService>(MockBehavior.Strict);
        integrity.Setup(x => x.Verify(
                "OffboardingRequest",
                "7",
                "HR User",
                signedAt,
                "base64-signature",
                "AABBCC"))
            .Returns(true);

        var result = SignatureIntegrityGuard.IsValid(
            integrity.Object,
            "OffboardingRequest",
            "7",
            "HR User",
            "Unknown",
            signedAt,
            "base64-signature",
            "AABBCC");

        Assert.True(result);
        integrity.VerifyAll();
    }
}

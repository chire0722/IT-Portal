using System.Security.Cryptography;
using System.Text;
using IT_Form.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IT_Form.Tests;

public sealed class SignatureIntegrityServiceTests
{
    [Fact]
    public void ComputeHmac_UsesActiveKeyAndVersionedFormat()
    {
        var activeKey = RandomNumberGenerator.GetBytes(32);
        var service = CreateService(activeKey);
        var signedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("signature"));

        var actual = service.ComputeHmac("OnboardingRequest", "7", "user-2", signedAt, signature);
        var expected = ComputeHmac(activeKey, "OnboardingRequest", "7", "user-2", signedAt, signature);

        Assert.Equal("v2:" + expected, actual);
        Assert.True(service.Verify("OnboardingRequest", "7", "user-2", signedAt, signature, actual));
    }

    [Fact]
    public void Verify_RejectsUnversionedHistoricalHmacAfterKeyExposure()
    {
        var activeKey = RandomNumberGenerator.GetBytes(32);
        var exposedKey = RandomNumberGenerator.GetBytes(32);
        var service = CreateService(activeKey);
        var signedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("signature"));
        var historicalHmac = ComputeHmac(exposedKey, "OffboardingRequest", "42", "user-1", signedAt, signature);

        Assert.False(service.Verify("OffboardingRequest", "42", "user-1", signedAt, signature, historicalHmac));
    }

    [Fact]
    public void Verify_RejectsLegacyImportPrefixWithoutCryptographicProof()
    {
        var service = CreateService(RandomNumberGenerator.GetBytes(32));
        var signedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.False(service.Verify("OnboardingRequest", "7", "user-2", signedAt, "c2ln", "SYSTEM-IMPORT-trusted"));
    }

    [Fact]
    public void Verify_RejectsSignatureThatDoesNotMatchActiveKey()
    {
        var service = CreateService(RandomNumberGenerator.GetBytes(32));
        var signedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.False(service.Verify("OnboardingRequest", "7", "user-2", signedAt, "c2ln", "v2:" + new string('A', 64)));
    }

    [Fact]
    public void Constructor_RequiresActiveKeyOutsideDevelopment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns("Production");
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new SignatureIntegrityService(configuration, environment.Object));
    }

    private static SignatureIntegrityService CreateService(byte[] activeKey)
    {
        var values = new Dictionary<string, string?>
        {
            ["SignatureHmac:Key"] = Convert.ToBase64String(activeKey)
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(Environments.Production);
        return new SignatureIntegrityService(configuration, environment.Object);
    }

    private static string ComputeHmac(byte[] key, string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64)
    {
        var normalizedDate = DateTime.SpecifyKind(signedAtUtc, DateTimeKind.Utc);
        var message = $"{entityType}|{entityId}|{signerUserId}|{normalizedDate:O}|{signatureBase64}";
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
    }
}

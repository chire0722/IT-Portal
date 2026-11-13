using System.Security.Cryptography;
using IT_Form.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace IT_Form.Tests;

public sealed class SignatureIntegrityServiceRotationTests
{
    private static readonly DateTime SignedAt = new(2026, 11, 10, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeHmac_UsesCurrentVersionPrefix()
    {
        var service = CreateService("v2", ("v1", NewKey()), ("v2", NewKey()));

        var hmac = service.ComputeHmac("OnboardingRequest", "42", "hr", SignedAt, "signature");

        Assert.StartsWith("v2:", hmac, StringComparison.Ordinal);
        Assert.True(service.Verify("OnboardingRequest", "42", "hr", SignedAt, "signature", hmac));
    }

    [Fact]
    public void Verify_AcceptsPreviousVersionAfterRotation()
    {
        var v1 = NewKey();
        var v2 = NewKey();
        var beforeRotation = CreateService("v1", ("v1", v1));
        var historicSignature = beforeRotation.ComputeHmac("OffboardingRequest", "9", "employee", SignedAt, "signature");
        var afterRotation = CreateService("v2", ("v1", v1), ("v2", v2));

        Assert.True(afterRotation.Verify("OffboardingRequest", "9", "employee", SignedAt, "signature", historicSignature));
        Assert.False(afterRotation.Verify("OffboardingRequest", "9", "employee", SignedAt, "tampered", historicSignature));
    }

    [Fact]
    public void Verify_AcceptsUnversionedLegacyHmacDuringMigration()
    {
        var legacyKey = NewKey();
        var legacy = CreateLegacyService(legacyKey);
        var historicSignature = legacy.ComputeHmac("OnboardingRequest", "17", "hr", SignedAt, "signature");
        var rotated = CreateService("v2", ("v1", legacyKey), ("v2", NewKey()));

        Assert.DoesNotContain(":", historicSignature);
        Assert.True(rotated.Verify("OnboardingRequest", "17", "hr", SignedAt, "signature", historicSignature));
        Assert.False(rotated.Verify("OnboardingRequest", "17", "hr", SignedAt, "signature", "v9:" + historicSignature));
    }

    [Fact]
    public void Constructor_RejectsCurrentVersionWithoutKey()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SignatureHmac:CurrentVersion"] = "v2",
                ["SignatureHmac:Keys:v1"] = Convert.ToBase64String(NewKey())
            })
            .Build();
        var environment = ProductionEnvironment();

        Assert.Throws<InvalidOperationException>(() => new SignatureIntegrityService(configuration, environment.Object));
    }

    private static SignatureIntegrityService CreateService(string currentVersion, params (string Version, byte[] Key)[] keys)
    {
        var values = new Dictionary<string, string?>
        {
            ["SignatureHmac:CurrentVersion"] = currentVersion
        };
        foreach (var (version, key) in keys)
        {
            values[$"SignatureHmac:Keys:{version}"] = Convert.ToBase64String(key);
        }

        return new SignatureIntegrityService(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            ProductionEnvironment().Object);
    }

    private static SignatureIntegrityService CreateLegacyService(byte[] key) =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SignatureHmac:Key"] = Convert.ToBase64String(key)
                })
                .Build(),
            ProductionEnvironment().Object);

    private static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    private static Mock<IWebHostEnvironment> ProductionEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Production);
        return environment;
    }
}

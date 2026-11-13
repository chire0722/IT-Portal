using System.Security.Cryptography;
using IT_Form.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace IT_Form.Tests;

public sealed class SignatureIntegrityServiceLegacyMarkerTests
{
    [Fact]
    public void Verify_RejectsSyntheticSystemImportMarker()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SignatureHmac:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            })
            .Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Production);
        var service = new SignatureIntegrityService(configuration, environment.Object);

        var result = service.Verify(
            "OnboardingRequest",
            "42",
            "HR User",
            DateTime.UtcNow,
            "base64-signature",
            "SYSTEM-IMPORT-LEGACY");

        Assert.False(result);
    }
}

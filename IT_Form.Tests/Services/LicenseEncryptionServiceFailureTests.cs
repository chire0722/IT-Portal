using System.IO;
using System.Security.Cryptography;
using IT_Form.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Tests.Services;

public sealed class LicenseEncryptionServiceFailureTests
{
    [Fact]
    public void DecryptKey_ReturnsPlaintext_WhenMatchingKeyRingIsAvailable()
    {
        var root = Path.Combine(Path.GetTempPath(), $"it-portal-dp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(root));
            var service = new LicenseEncryptionService(new ConfigurationBuilder().Build(), provider);

            var encrypted = service.EncryptKey("test-license-secret");

            Assert.StartsWith("dp:v2:", encrypted);
            Assert.Equal("test-license-secret", service.DecryptKey(encrypted));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void DecryptKey_Throws_WhenDataProtectionKeyRingDoesNotMatch()
    {
        var writerRoot = Path.Combine(Path.GetTempPath(), $"it-portal-dp-writer-{Guid.NewGuid():N}");
        var readerRoot = Path.Combine(Path.GetTempPath(), $"it-portal-dp-reader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(writerRoot);
        Directory.CreateDirectory(readerRoot);

        try
        {
            var configuration = new ConfigurationBuilder().Build();
            var writer = new LicenseEncryptionService(
                configuration,
                DataProtectionProvider.Create(new DirectoryInfo(writerRoot)));
            var reader = new LicenseEncryptionService(
                configuration,
                DataProtectionProvider.Create(new DirectoryInfo(readerRoot)));

            var encrypted = writer.EncryptKey("test-license-secret");

            Assert.Throws<CryptographicException>(() => reader.DecryptKey(encrypted));
        }
        finally
        {
            if (Directory.Exists(writerRoot))
            {
                Directory.Delete(writerRoot, recursive: true);
            }

            if (Directory.Exists(readerRoot))
            {
                Directory.Delete(readerRoot, recursive: true);
            }
        }
    }
}

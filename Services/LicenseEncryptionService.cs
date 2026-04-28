// Project comment: LicenseEncryptionService.cs is part of the IT Form application source code.

using System.Security.Cryptography;
using System.Text;
using IT_Form.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides ILicenseEncryption service behavior for the application.
    /// </summary>
    public interface ILicenseEncryptionService
    {
        string EncryptKey(string plainText);
        string DecryptKey(string cipherText);
    }

    /// <summary>
    /// Provides LicenseEncryption service behavior for the application.
    /// </summary>
    public class LicenseEncryptionService : ILicenseEncryptionService
    {
        private const string DataProtectionPrefix = "dp:v2:";

        private readonly IDataProtector _protector;
        private readonly byte[]? _legacyKey;
        private readonly byte[]? _legacyIv;

        public LicenseEncryptionService(IConfiguration configuration)
            : this(
                configuration,
                DataProtectionProvider.Create(
                    new DirectoryInfo(Path.Combine(Path.GetTempPath(), "IT_Form_DataProtection_TestKeys"))))
        {
        }

        public LicenseEncryptionService(
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("IT_Form.SoftwareLicenseKeys.v2");

            var legacyKey = ConfigurationValueNormalizer.NormalizeSecret(configuration["Security:LicenseEncryptionKey"]);
            var legacyIv = ConfigurationValueNormalizer.NormalizeSecret(configuration["Security:LicenseEncryptionIv"]);
            if (!string.IsNullOrWhiteSpace(legacyKey) && !string.IsNullOrWhiteSpace(legacyIv))
            {
                _legacyKey = Encoding.UTF8.GetBytes(legacyKey.PadRight(32)[..32]);
                _legacyIv = Encoding.UTF8.GetBytes(legacyIv.PadRight(16)[..16]);
            }
        }

        /// <summary>
        /// Runs the EncryptKey operation.
        /// </summary>
        public string EncryptKey(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            return DataProtectionPrefix + _protector.Protect(plainText);
        }

        /// <summary>
        /// Runs the DecryptKey operation.
        /// </summary>
        public string DecryptKey(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            if (cipherText.StartsWith(DataProtectionPrefix, StringComparison.Ordinal))
            {
                try
                {
                    return _protector.Unprotect(cipherText[DataProtectionPrefix.Length..]);
                }
                catch (CryptographicException)
                {
                    return cipherText;
                }
            }

            if (_legacyKey == null || _legacyIv == null)
            {
                return cipherText;
            }

            try
            {
                return DecryptLegacyAes(cipherText);
            }
            catch (CryptographicException)
            {
                return cipherText;
            }
            catch (FormatException)
            {
                return cipherText;
            }
        }

        /// <summary>
        /// Runs the DecryptLegacyAes operation.
        /// </summary>
        private string DecryptLegacyAes(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = _legacyKey!;
                aes.IV = _legacyIv!;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}

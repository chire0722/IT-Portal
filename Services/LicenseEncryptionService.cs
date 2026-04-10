using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Services
{
    public interface ILicenseEncryptionService
    {
        string EncryptKey(string plainText);
        string DecryptKey(string cipherText);
    }

    public class LicenseEncryptionService : ILicenseEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public LicenseEncryptionService(IConfiguration configuration)
        {
            var configKey = configuration["Security:LicenseEncryptionKey"] ?? "AntigravityDefaultKey32BytesLength!";
            var configIv = configuration["Security:LicenseEncryptionIv"] ?? "AntigravityDefaultIV16Bytes!";

            _key = Encoding.UTF8.GetBytes(configKey.PadRight(32).Substring(0, 32));
            _iv = Encoding.UTF8.GetBytes(configIv.PadRight(16).Substring(0, 16));
        }

        public string EncryptKey(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string DecryptKey(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

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
            catch
            {
                // If decryption fails (e.g., transition from plain text or wrong key), return raw text for safety during migration
                return cipherText;
            }
        }
    }
}

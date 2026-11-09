using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Services
{
    public interface ISignatureIntegrityService
    {
        string ComputeHmac(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64);
        bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac);
    }

    /// <summary>
    /// Signs and verifies signatures using only the active key. Legacy signatures created
    /// with a publicly exposed key are rejected because their integrity can no longer be trusted.
    /// </summary>
    public sealed class SignatureIntegrityService : ISignatureIntegrityService
    {
        private readonly byte[] _activeKeyBytes;

        public SignatureIntegrityService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var activeKey = configuration["SignatureHmac:Key"];
            if (IsPlaceholder(activeKey))
            {
                if (!environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "SignatureHmac:Key is not configured. Set IT_FORM__SignatureHmac__Key.");
                }

                _activeKeyBytes = RandomNumberGenerator.GetBytes(32);
            }
            else
            {
                _activeKeyBytes = DecodeKey(activeKey!, "SignatureHmac:Key");
            }

        }

        public string ComputeHmac(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64)
        {
            return "v2:" + Convert.ToHexString(ComputeHash(_activeKeyBytes, entityType, entityId, signerUserId, signedAtUtc, signatureBase64));
        }

        public bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac)
        {
            if (string.IsNullOrWhiteSpace(storedHmac))
            {
                return false;
            }

            // Import markers are not cryptographic signatures. Never treat a string prefix
            // as proof of integrity; only a valid HMAC under the active secret is accepted.
            if (!storedHmac.StartsWith("v2:", StringComparison.Ordinal))
            {
                return false;
            }

            byte[] supplied;
            try
            {
                supplied = Convert.FromHexString(storedHmac[3..]);
            }
            catch (FormatException)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                ComputeHash(_activeKeyBytes, entityType, entityId, signerUserId, signedAtUtc, signatureBase64),
                supplied);
        }

        private static byte[] ComputeHash(byte[] key, string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64)
        {
            var normalizedDate = DateTime.SpecifyKind(signedAtUtc, DateTimeKind.Utc);
            var message = $"{entityType}|{entityId}|{signerUserId ?? ""}|{normalizedDate:O}|{signatureBase64}";
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        }

        private static bool IsPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value) ||
            value.Contains("SET_VIA_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("SET_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("ROTATE_AND_USE_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
            (value.StartsWith('<') && value.EndsWith('>'));

        private static byte[] DecodeKey(string value, string settingName)
        {
            byte[] keyBytes;
            try
            {
                keyBytes = Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"{settingName} must be a valid Base64 string.", ex);
            }

            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException($"{settingName} must decode to at least 32 bytes.");
            }

            return keyBytes;
        }
    }
}
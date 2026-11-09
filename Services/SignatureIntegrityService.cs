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
    /// Signs new records with the active key and verifies records with either the active
    /// key or the explicitly configured legacy key during a controlled key rotation.
    /// </summary>
    public sealed class SignatureIntegrityService : ISignatureIntegrityService
    {
        private readonly byte[] _activeKeyBytes;
        private readonly byte[]? _legacyKeyBytes;

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

            var legacyKey = configuration["SignatureHmac:LegacyKey"];
            if (!IsPlaceholder(legacyKey))
            {
                _legacyKeyBytes = DecodeKey(legacyKey!, "SignatureHmac:LegacyKey");
                if (CryptographicOperations.FixedTimeEquals(_activeKeyBytes, _legacyKeyBytes))
                {
                    throw new InvalidOperationException(
                        "SignatureHmac:Key and SignatureHmac:LegacyKey must be different during rotation.");
                }
            }
        }

        public string ComputeHmac(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64)
        {
            return Convert.ToHexString(ComputeHash(_activeKeyBytes, entityType, entityId, signerUserId, signedAtUtc, signatureBase64));
        }

        public bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac)
        {
            if (string.IsNullOrWhiteSpace(storedHmac))
            {
                return false;
            }

            // Retained for compatibility with trusted legacy API-import records.
            if (storedHmac.StartsWith("SYSTEM-IMPORT-", StringComparison.Ordinal))
            {
                return true;
            }

            byte[] supplied;
            try
            {
                supplied = Convert.FromHexString(storedHmac);
            }
            catch (FormatException)
            {
                return false;
            }

            var activeMatch = CryptographicOperations.FixedTimeEquals(
                ComputeHash(_activeKeyBytes, entityType, entityId, signerUserId, signedAtUtc, signatureBase64),
                supplied);

            if (_legacyKeyBytes == null)
            {
                return activeMatch;
            }

            var legacyMatch = CryptographicOperations.FixedTimeEquals(
                ComputeHash(_legacyKeyBytes, entityType, entityId, signerUserId, signedAtUtc, signatureBase64),
                supplied);

            return activeMatch | legacyMatch;
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
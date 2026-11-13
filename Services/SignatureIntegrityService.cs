// Project comment: SignatureIntegrityService.cs is part of the IT Form application source code.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides HMAC-SHA256 based integrity verification for digital signatures.
    /// When a user signs a form, a MAC is computed over the canonical data and stored alongside the signature.
    /// This addresses H-03 from the security audit: prevents a rogue admin from swapping one 
    /// signature image for another without detection.
    /// 
    /// PDPA 2010 Principle 4 (Security) / CISSP Cryptography Domain.
    /// </summary>
    public interface ISignatureIntegrityService
    {
        /// <summary>
        /// Compute a HMAC-SHA256 for a signing event.
        /// </summary>
        /// <param name="entityType">e.g. "OffboardingRequest"</param>
        /// <param name="entityId">Primary key of the signed record</param>
        /// <param name="signerUserId">The actor who signed (UserName)</param>
        /// <param name="signedAtUtc">The exact UTC timestamp of signing</param>
        /// <param name="signatureBase64">The raw base64 signature image</param>
        /// <returns>Hex-encoded HMAC string to persist in DB</returns>
        string ComputeHmac(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64);

        /// <summary>
        /// Verify a previously stored HMAC matches the current data.
        /// Returns false if tampered.
        /// </summary>
        bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac);
    }

    /// <summary>
    /// Provides SignatureIntegrity service behavior for the application.
    /// </summary>
    public class SignatureIntegrityService : ISignatureIntegrityService
    {
        private readonly IReadOnlyDictionary<string, byte[]> _keys;
        private readonly string _currentVersion;
        private readonly bool _usesVersionedKeyRing;

        public SignatureIntegrityService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            // New deployments should configure SignatureHmac:CurrentVersion and one or more
            // SignatureHmac:Keys:<version> values. The legacy single Key setting remains
            // supported so an existing installation can migrate without invalidating records.
            var configuredKeys = configuration.GetSection("SignatureHmac:Keys")
                .GetChildren()
                .ToDictionary(
                    child => child.Key,
                    child => DecodeKey(child.Value, $"SignatureHmac:Keys:{child.Key}"),
                    StringComparer.OrdinalIgnoreCase);

            if (configuredKeys.Count > 0)
            {
                foreach (var version in configuredKeys.Keys)
                {
                    ValidateVersion(version);
                }

                _currentVersion = configuration["SignatureHmac:CurrentVersion"] ?? string.Empty;
                ValidateVersion(_currentVersion);
                if (!configuredKeys.ContainsKey(_currentVersion))
                {
                    throw new InvalidOperationException(
                        $"SignatureHmac:CurrentVersion '{_currentVersion}' has no configured key.");
                }

                _keys = configuredKeys;
                _usesVersionedKeyRing = true;
                return;
            }

            var legacyKey = configuration["SignatureHmac:Key"];
            if (IsMissingOrPlaceholder(legacyKey))
            {
                if (environment.IsDevelopment())
                {
                    legacyKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                }
                else
                {
                    throw new InvalidOperationException(
                        "SignatureHmac is not configured. Set SignatureHmac:CurrentVersion with " +
                        "SignatureHmac:Keys:<version>, or the legacy IT_FORM__SignatureHmac__Key during migration.");
                }
            }

            _keys = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["legacy"] = DecodeKey(legacyKey, "SignatureHmac:Key")
            };
            _currentVersion = "legacy";
            _usesVersionedKeyRing = false;
        }

        /// <summary>
        /// Runs the ComputeHmac operation.
        /// </summary>
        public string ComputeHmac(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64)
        {
            // Fix DateTimeKind loss from SQLite which drops the trailing 'Z' when loaded from DB
            var normalizedDate = DateTime.SpecifyKind(signedAtUtc, DateTimeKind.Utc);

            // Canonical message: pipe-delimited to prevent concatenation attacks
            var message = $"{entityType}|{entityId}|{signerUserId ?? ""}|{normalizedDate:O}|{signatureBase64}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            var hash = ComputeHash(_keys[_currentVersion], messageBytes);
            var encoded = Convert.ToHexString(hash); // uppercase hex
            return _usesVersionedKeyRing
                ? $"{_currentVersion}:{encoded}"
                : encoded;
        }

        /// <summary>
        /// Runs the Verify operation.
        /// </summary>
        public bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac)
        {
            if (string.IsNullOrEmpty(storedHmac)) return false;

            // Every non-empty signature HMAC must be a real cryptographic MAC.
            // Synthetic import markers are not proof of integrity and must never bypass verification.

            var normalizedDate = DateTime.SpecifyKind(signedAtUtc, DateTimeKind.Utc);
            var message = $"{entityType}|{entityId}|{signerUserId ?? ""}|{normalizedDate:O}|{signatureBase64}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            var separator = storedHmac.IndexOf(':');
            var suppliedVersion = separator > 0 ? storedHmac[..separator] : null;
            var suppliedHex = separator > 0 ? storedHmac[(separator + 1)..] : storedHmac;
            if (separator == 0 || (separator > 0 && separator != storedHmac.LastIndexOf(':')))
            {
                return false;
            }

            if (!TryDecodeHex(suppliedHex, out var suppliedBytes))
            {
                return false;
            }

            IEnumerable<byte[]> candidateKeys = suppliedVersion != null
                ? _keys.TryGetValue(suppliedVersion, out var versionedKey)
                    ? [versionedKey]
                    : []
                : _keys.Values;

            var matched = false;
            foreach (var key in candidateKeys)
            {
                var computedBytes = ComputeHash(key, messageBytes);
                // Evaluate every configured key for an unversioned record so key presence
                // cannot be inferred from early-return timing during migration.
                matched |= CryptographicOperations.FixedTimeEquals(computedBytes, suppliedBytes);
            }

            return matched;
        }

        private static byte[] ComputeHash(byte[] key, byte[] messageBytes)
        {
            using var hmac = new HMACSHA256(key);
            return hmac.ComputeHash(messageBytes);
        }

        private static bool TryDecodeHex(string value, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromHexString(value);
                return bytes.Length == 32;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }

        private static byte[] DecodeKey(string? value, string settingName)
        {
            if (IsMissingOrPlaceholder(value))
            {
                throw new InvalidOperationException($"{settingName} is not configured.");
            }

            try
            {
                var bytes = Convert.FromBase64String(value!);
                if (bytes.Length < 32)
                {
                    throw new InvalidOperationException($"{settingName} must decode to at least 32 bytes.");
                }

                return bytes;
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"{settingName} is not a valid Base64 string.", ex);
            }
        }

        private static bool IsMissingOrPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value) ||
            value.Contains("SET_VIA_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("SET_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("ROTATE_AND_USE_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
            (value.StartsWith('<') && value.EndsWith('>'));

        private static void ValidateVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version) || version.Length > 32 ||
                version.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_' or '.')))
            {
                throw new InvalidOperationException(
                    "SignatureHmac key versions must be 1-32 characters containing only letters, digits, '.', '_' or '-'.");
            }
        }
    }
}

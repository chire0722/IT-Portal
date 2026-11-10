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
        private readonly byte[] _keyBytes;

        public SignatureIntegrityService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Read HMAC key from configuration — must be at least 32 bytes.
            // Add "SignatureHmac:Key" to appsettings.json or a "IT_FORM__SignatureHmac__Key" machine environment variable.
            var keyStr = configuration["SignatureHmac:Key"];

            if (string.IsNullOrWhiteSpace(keyStr) ||
                keyStr.Contains("SET_VIA_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
                keyStr.Contains("SET_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase) ||
                keyStr.Contains("ROTATE_AND_USE_ENV_VAR", StringComparison.OrdinalIgnoreCase) ||
                (keyStr.StartsWith('<') && keyStr.EndsWith('>')))
            {
                if (environment.IsDevelopment())
                {
                    _keyBytes = RandomNumberGenerator.GetBytes(32);
                    return;
                }

                throw new InvalidOperationException(
                    "SignatureHmac:Key is not configured. " +
                    "Please set the 'IT_FORM__SignatureHmac__Key' machine environment variable.");
            }

            try
            {
                _keyBytes = Convert.FromBase64String(keyStr);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "SignatureHmac:Key is not a valid Base64 string. Check your environment variables.", ex);
            }

            if (_keyBytes.Length < 32)
            {
                throw new InvalidOperationException("SignatureHmac:Key must decode to at least 32 bytes.");
            }
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

            using var hmac = new HMACSHA256(_keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return Convert.ToHexString(hashBytes); // uppercase hex
        }

        /// <summary>
        /// Runs the Verify operation.
        /// </summary>
        public bool Verify(string entityType, string entityId, string signerUserId, DateTime signedAtUtc, string signatureBase64, string storedHmac)
        {
            if (string.IsNullOrEmpty(storedHmac)) return false;

            // Every non-empty signature HMAC must be a real cryptographic MAC.
            // Synthetic import markers are not proof of integrity and must never bypass verification.

            try
            {
                var computed = ComputeHmac(entityType, entityId, signerUserId, signedAtUtc, signatureBase64);
                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(computed),
                    Convert.FromHexString(storedHmac));
            }
            catch (FormatException)
            {
                return false; // Malformed non-hex string stored in HMAC
            }
        }
    }
}

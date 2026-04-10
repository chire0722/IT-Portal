using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using IT_Form.Models;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace IT_Form.Security
{
    public class Argon2idPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        private const byte IdentityFormatMarker = 0x01;
        private const byte Argon2VersionedFormatMarker = 0x02;

        private readonly Argon2idSettings _settings;
        private readonly Argon2idSettings _legacySettings;
        private readonly PasswordHasher<TUser> _fallbackPasswordHasher = new();

        public Argon2idPasswordHasher(IOptions<SecuritySettings> options)
        {
            _settings = options.Value.Argon2id;
            _legacySettings = options.Value.LegacyArgon2id;
        }

        public string HashPassword(TUser user, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(_settings.SaltSize);
            byte[] hash = ComputeHash(password, salt, _settings);

            byte[] result = new byte[14 + _settings.SaltSize + _settings.HashSize];
            result[0] = Argon2VersionedFormatMarker;
            result[1] = checked((byte)_settings.DegreeOfParallelism);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(2, 4), _settings.MemorySize);
            BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(6, 4), _settings.Iterations);
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(10, 2), checked((ushort)_settings.SaltSize));
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(12, 2), checked((ushort)_settings.HashSize));
            Buffer.BlockCopy(salt, 0, result, 14, _settings.SaltSize);
            Buffer.BlockCopy(hash, 0, result, 14 + _settings.SaltSize, _settings.HashSize);

            return Convert.ToBase64String(result);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || providedPassword == null)
            {
                return PasswordVerificationResult.Failed;
            }

            try
            {
                byte[] decoded = Convert.FromBase64String(hashedPassword);

                if (TryVerifyVersionedArgon2(decoded, providedPassword, out var versionedResult))
                {
                    return versionedResult;
                }

                if (TryVerifyLegacyArgon2(decoded, providedPassword, out var legacyResult))
                {
                    return legacyResult;
                }

                var fallbackResult = _fallbackPasswordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
                return fallbackResult switch
                {
                    PasswordVerificationResult.Success => PasswordVerificationResult.SuccessRehashNeeded,
                    PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
                    _ => PasswordVerificationResult.Failed
                };
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private bool TryVerifyVersionedArgon2(
            byte[] decoded,
            string providedPassword,
            out PasswordVerificationResult result)
        {
            result = PasswordVerificationResult.Failed;

            if (decoded.Length < 14 || decoded[0] != Argon2VersionedFormatMarker)
            {
                return false;
            }

            var parsedSettings = new Argon2idSettings
            {
                DegreeOfParallelism = decoded[1],
                MemorySize = BinaryPrimitives.ReadInt32LittleEndian(decoded.AsSpan(2, 4)),
                Iterations = BinaryPrimitives.ReadInt32LittleEndian(decoded.AsSpan(6, 4)),
                SaltSize = BinaryPrimitives.ReadUInt16LittleEndian(decoded.AsSpan(10, 2)),
                HashSize = BinaryPrimitives.ReadUInt16LittleEndian(decoded.AsSpan(12, 2))
            };

            if (!IsSane(parsedSettings) || decoded.Length != 14 + parsedSettings.SaltSize + parsedSettings.HashSize)
            {
                return true;
            }

            byte[] salt = decoded.AsSpan(14, parsedSettings.SaltSize).ToArray();
            byte[] hash = decoded.AsSpan(14 + parsedSettings.SaltSize, parsedSettings.HashSize).ToArray();
            byte[] providedHash = ComputeHash(providedPassword, salt, parsedSettings);

            if (!CryptographicOperations.FixedTimeEquals(hash, providedHash))
            {
                result = PasswordVerificationResult.Failed;
                return true;
            }

            result = AreEquivalent(parsedSettings, _settings)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.SuccessRehashNeeded;
            return true;
        }

        private bool TryVerifyLegacyArgon2(
            byte[] decoded,
            string providedPassword,
            out PasswordVerificationResult result)
        {
            result = PasswordVerificationResult.Failed;

            var expectedLength = 1 + _legacySettings.SaltSize + _legacySettings.HashSize;
            if (decoded.Length != expectedLength || decoded[0] != IdentityFormatMarker)
            {
                return false;
            }

            byte[] salt = decoded.AsSpan(1, _legacySettings.SaltSize).ToArray();
            byte[] hash = decoded.AsSpan(1 + _legacySettings.SaltSize, _legacySettings.HashSize).ToArray();
            byte[] providedHash = ComputeHash(providedPassword, salt, _legacySettings);

            result = CryptographicOperations.FixedTimeEquals(hash, providedHash)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;

            return true;
        }

        private static byte[] ComputeHash(string password, byte[] salt, Argon2idSettings settings)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = settings.DegreeOfParallelism,
                MemorySize = settings.MemorySize,
                Iterations = settings.Iterations
            };

            return argon2.GetBytes(settings.HashSize);
        }

        private static bool AreEquivalent(Argon2idSettings left, Argon2idSettings right)
            => left.DegreeOfParallelism == right.DegreeOfParallelism
               && left.MemorySize == right.MemorySize
               && left.Iterations == right.Iterations
               && left.SaltSize == right.SaltSize
               && left.HashSize == right.HashSize;

        private static bool IsSane(Argon2idSettings settings)
            => settings.DegreeOfParallelism > 0
               && settings.MemorySize > 0
               && settings.Iterations > 0
               && settings.SaltSize is >= 8 and <= 1024
               && settings.HashSize is >= 16 and <= 1024;
    }
}

// Project comment: SecuritySettings.cs is part of the IT Form application source code.

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SecuritySettings class used by the IT Form application.
    /// </summary>
    public class SecuritySettings
    {
        public int MaxOtpAttempts { get; set; } = 5;
        public List<int> PasswordLockoutMinutes { get; set; } = [5, 15, 30, 120];
        public int OtpExpiryMinutes { get; set; } = 3;
        public int DeviceVerificationExpiryMinutes { get; set; } = 30;
        public bool AllowLegacyInventoryApiKey { get; set; } = false;
        public Argon2idSettings Argon2id { get; set; } = new();
        public Argon2idSettings LegacyArgon2id { get; set; } = new()
        {
            DegreeOfParallelism = 8,
            MemorySize = 65536,
            Iterations = 4,
            SaltSize = 16,
            HashSize = 32
        };
    }

    /// <summary>
    /// Defines the Argon2idSettings class used by the IT Form application.
    /// </summary>
    public class Argon2idSettings
    {
        public int DegreeOfParallelism { get; set; } = 4;
        public int MemorySize { get; set; } = 32768;
        public int Iterations { get; set; } = 3;
        public int SaltSize { get; set; } = 16;
        public int HashSize { get; set; } = 32;
    }
}

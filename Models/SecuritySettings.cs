namespace IT_Form.Models
{
    public class SecuritySettings
    {
        public List<string> AllowedDomains { get; set; } = new();
        public int MaxOtpAttempts { get; set; } = 5;
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

    public class Argon2idSettings
    {
        public int DegreeOfParallelism { get; set; } = 4;
        public int MemorySize { get; set; } = 32768;
        public int Iterations { get; set; } = 3;
        public int SaltSize { get; set; } = 16;
        public int HashSize { get; set; } = 32;
    }
}

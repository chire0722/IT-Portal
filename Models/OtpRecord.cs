// Project comment: OtpRecord.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the OtpRecord class used by the IT Form application.
    /// </summary>
    public class OtpRecord
    {
        public int Id { get; set; }

        [Required]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = IT_Form.Application.Features.Account.OtpPurposes.PasswordReset;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public int FailedAttempts { get; set; } = 0;

        public bool IsLocked { get; set; } = false;

        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

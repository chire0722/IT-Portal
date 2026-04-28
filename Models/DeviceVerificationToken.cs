// Project comment: DeviceVerificationToken.cs is part of the IT Form application source code.

using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the DeviceVerificationToken class used by the IT Form application.
    /// </summary>
    public class DeviceVerificationToken
    {
        [Key]
        [StringLength(100)]
        public string Token { get; set; } = Guid.NewGuid().ToString("N");

        [StringLength(450)] // IdentityUser PK length
        public string? EmployeeId { get; set; }

        // Navigation property
        public ApplicationUser? Employee { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }

        public bool HasHardwareFingerprintMismatch { get; set; }

        [StringLength(500)]
        public string? HardwareFingerprintWarning { get; set; }

        [StringLength(100)]
        public string? VerifiedAssetTag { get; set; }
    }
}

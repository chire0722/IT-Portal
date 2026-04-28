// Project comment: ApplicationUser.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the ApplicationUser class used by the IT Form application.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; } // e.g. "IT Executive", shown in email signatures

        /// <summary>
        /// Stores the base64-encoded email signature image (QR code, company logo block, etc.)
        /// </summary>
        public string? EmailSignatureImage { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Signature { get; set; } // Stores base64 signature image

        public List<UserTitle> UserTitles { get; set; } = new();

        public List<UserCompany> UserCompanies { get; set; } = new();

        /// <summary>
        /// Tracks consecutive lockouts to incrementally increase the lockout duration.
        /// </summary>
        public int LockoutCount { get; set; } = 0;

        /// <summary>
        /// When true, the user must change their password on next login.
        /// Set to true by IT Admin whenever a password is created or reset.
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        /// <summary>
        /// Tracks the last time a security alert for lockout was sent to prevent notification storms.
        /// </summary>
        public DateTime? LastLockoutAlertSentAt { get; set; }
    }
}

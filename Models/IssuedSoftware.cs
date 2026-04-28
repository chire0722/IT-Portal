// Project comment: IssuedSoftware.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the IssuedSoftware class used by the IT Form application.
    /// </summary>
    public class IssuedSoftware
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        public int SoftwareLicenseId { get; set; }
        public SoftwareLicense SoftwareLicense { get; set; } = null!;

        [StringLength(200)]
        public string? Notes { get; set; }

        // This is a snapshot of the name in case the original software is renamed/deleted
        [StringLength(100)]
        public string SoftwareName { get; set; } = string.Empty;
    }
}

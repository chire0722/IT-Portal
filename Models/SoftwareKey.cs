// Project comment: SoftwareKey.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT_Form.Attributes;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SoftwareKey class used by the IT Form application.
    /// </summary>
    public class SoftwareKey : ISoftDeletable
    {
        public int Id { get; set; }

        public int SoftwareLicenseId { get; set; }

        [ForeignKey("SoftwareLicenseId")]
        public virtual SoftwareLicense? SoftwareLicense { get; set; }

        [StringLength(1000)] // Extended for encrypted key values
        [EncryptColumn]
        public string? KeyValue { get; set; }

        [StringLength(64)]
        public string? KeyValueHash { get; set; }

        public bool IsAssigned { get; set; }

        public int? OnboardingRequestId { get; set; }

        [ForeignKey("OnboardingRequestId")]
        public virtual OnboardingRequest? OnboardingRequest { get; set; }

        public string? AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        public string? MachineName { get; set; }
        public DateTime? AssignmentDate { get; set; }

        public System.DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;

        // ISoftDeletable Implementation
        public bool IsDeleted { get; set; } = false;
        public System.DateTime? DeletedAt { get; set; }
        
        [StringLength(450)]
        public string? DeletedByUserId { get; set; }
    }
}

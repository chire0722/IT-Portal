using System.ComponentModel.DataAnnotations;
using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    public class SoftwareLicense : ISoftDeletable
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SoftwareName { get; set; } = string.Empty;

        public int ContractLimit { get; set; }
        public int TotalLicenses { get; set; }

        public int ExpiryAttemptCount { get; set; } = 0;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? VendorName { get; set; }

        [StringLength(256)]
        public string? SupportContact { get; set; }

        public virtual ICollection<SoftwareKey> Keys { get; set; } = new List<SoftwareKey>();

        // Link to StandardSoftware catalog entry
        [Required]
        public int StandardSoftwareId { get; set; }
        public StandardSoftware? StandardSoftware { get; set; }

        // Mapped to OnboardingRequest boolean properties internally by Controller
        [StringLength(50)]
        public string? SystemMappingKey { get; set; }

        public bool IsSubscription { get; set; }
        public bool IsFree { get; set; }

        public System.DateTime? ExpiryDate { get; set; }

        public ExpiryNotifyStatus ExpiryNotifyStatus { get; set; } = ExpiryNotifyStatus.Pending;

        // ISoftDeletable Implementation
        public bool IsDeleted { get; set; } = false;
        public System.DateTime? DeletedAt { get; set; }
        
        [StringLength(450)]
        public string? DeletedByUserId { get; set; }
    }
}


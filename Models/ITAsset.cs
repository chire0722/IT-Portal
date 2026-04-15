using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    public class ITAsset : ISoftDeletable
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string AssetTag { get; set; } = string.Empty;

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [Required]
        public AssetType AssetType { get; set; }

        public AssetStatus Status { get; set; } = AssetStatus.Available;

        [StringLength(200)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? WarrantyExpiry { get; set; }

        public ExpiryNotifyStatus WarrantyNotifyStatus { get; set; } = ExpiryNotifyStatus.Pending;
        public int ExpiryAttemptCount { get; set; } = 0;

        [StringLength(50)]
        public string? RAM { get; set; }

        [StringLength(50)]
        public string? Storage { get; set; }

        [StringLength(100)]
        public string? WindowsVersion { get; set; }

        [StringLength(20)]
        public string? DDRType { get; set; }

        [StringLength(200)]
        public string? CPU { get; set; }

        [StringLength(300)]
        public string? HardwareHash { get; set; }

        // Stores full hardware fingerprint JSON payload from audit agent.
        public string? HardwareAuditJson { get; set; }

        [StringLength(300)]
        public string? LatestHardwareHash { get; set; }

        // Stores the most recent hardware fingerprint JSON payload from the audit agent.
        public string? LatestHardwareAuditJson { get; set; }

        public bool HasHardwareFingerprintMismatch { get; set; }

        public DateTime? HardwareFingerprintMismatchDetectedAt { get; set; }

        [StringLength(500)]
        public string? HardwareFingerprintWarning { get; set; }

        public DateTime? LastSeen { get; set; }

        public List<MaintenanceLog> MaintenanceLogs { get; set; } = new();

        // One-to-many relationship with IssuedAsset (history of who it was issued to)
        public List<IssuedAsset> IssuedHistory { get; set; } = new();
        public bool NoExtendWarranty { get; set; } = false;
        
        [StringLength(100)]
        public string? OtherAssetType { get; set; }

        // ISoftDeletable Implementation
        public bool IsDeleted { get; set; } = false;
        public System.DateTime? DeletedAt { get; set; }
        
        [StringLength(450)]
        public string? DeletedByUserId { get; set; }
    }
}

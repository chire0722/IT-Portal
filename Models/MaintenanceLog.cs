using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class MaintenanceLog : ISoftDeletable
    {
        public int Id { get; set; }

        public int ITAssetId { get; set; }
        public ITAsset? ITAsset { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime ServiceDate { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ServiceProvider { get; set; }

        public decimal? Cost { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}

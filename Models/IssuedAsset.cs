using System.ComponentModel.DataAnnotations;
using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    public class IssuedAsset
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        [StringLength(200)]
        public string? CustomEmployeeName { get; set; }

        public int? ITAssetId { get; set; }
        public ITAsset? ITAsset { get; set; }

        [Required]
        public AssetType AssetType { get; set; }

        [StringLength(100)]
        public string? OtherAssetType { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? AssetTag { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsChecked { get; set; }

        public int? Qty { get; set; }

        [StringLength(200)]
        public string? BrandModel { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class RequestSoftware
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        public int StandardSoftwareId { get; set; }
        public StandardSoftware StandardSoftware { get; set; } = null!;

        [StringLength(100)]
        public string SoftwareName { get; set; } = string.Empty;
    }
}

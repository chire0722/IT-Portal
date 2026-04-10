using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class SigningToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Unique token used in the URL</summary>
        [Required]
        public string Token { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>The onboarding request this token belongs to</summary>
        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        /// <summary>The offboarding request this token belongs to</summary>
        public int? OffboardingRequestId { get; set; }
        public OffboardingRequest? OffboardingRequest { get; set; }

        /// <summary>When the token expires</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Whether the token has been used</summary>
        public bool IsUsed { get; set; }

        /// <summary>When the token was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

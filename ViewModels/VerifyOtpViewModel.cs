using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-character OTP code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 characters.")]
        [RegularExpression(@"^[A-HJ-NP-Za-hj-np-z2-9]{6}$", ErrorMessage = "OTP must contain only letters and numbers.")]
        [Display(Name = "OTP Code")]
        public string Code { get; set; } = string.Empty;

        public int RemainingCooldownSeconds { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your username or email address.")]
        [Display(Name = "Username or Email")]
        public string Username { get; set; } = string.Empty;
    }
}

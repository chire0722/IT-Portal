// Project comment: ForgotPasswordViewModel.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the ForgotPassword screen or form.
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your username or email address.")]
        [Display(Name = "Username or Email")]
        public string Username { get; set; } = string.Empty;
    }
}

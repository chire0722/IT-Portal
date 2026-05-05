// Project comment: LoginViewModel.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the Login screen or form.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}

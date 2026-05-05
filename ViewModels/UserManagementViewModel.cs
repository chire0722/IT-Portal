// Project comment: UserManagementViewModel.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the UserManagement screen or form.
    /// </summary>
    public class UserManagementViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        // Title checkboxes
        [Display(Name = "IT")]
        public bool IsIT { get; set; }

        [Display(Name = "HR")]
        public bool IsHR { get; set; }

        [Display(Name = "HOD")]
        public bool IsHOD { get; set; }

        [Display(Name = "Super Admin")]
        public bool IsSuperAdmin { get; set; }

        public bool IsActive { get; set; } = true;

        // Company checkboxes (Dynamic)
        public List<string> SelectedCompanies { get; set; } = new List<string>();
    }
}

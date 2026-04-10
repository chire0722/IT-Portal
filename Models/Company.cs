using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Lot / Address")]
        public string? Lot { get; set; }

        [Display(Name = "Logo Path")]
        public string? LogoPath { get; set; }

        [Display(Name = "Use Logo on Login Page")]
        public bool IsLoginPageLogo { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Name")]
        public string DisplayName => string.IsNullOrEmpty(Lot) ? Name : $"{Name} ({Lot})";
    }
}

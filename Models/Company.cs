using System.ComponentModel.DataAnnotations;
using IT_Form.Helpers;

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

        [Required]
        [StringLength(100)]
        [Display(Name = "Time Zone")]
        public string TimeZoneId { get; set; } = CompanyTimeZoneHelper.DefaultTimeZoneId;

        [Display(Name = "Business Start Time")]
        public TimeSpan BusinessStartTime { get; set; } = new(8, 30, 0);

        [Display(Name = "Business End Time")]
        public TimeSpan BusinessEndTime { get; set; } = new(17, 30, 0);

        [Display(Name = "Works On Monday")]
        public bool WorksOnMonday { get; set; } = true;

        [Display(Name = "Works On Tuesday")]
        public bool WorksOnTuesday { get; set; } = true;

        [Display(Name = "Works On Wednesday")]
        public bool WorksOnWednesday { get; set; } = true;

        [Display(Name = "Works On Thursday")]
        public bool WorksOnThursday { get; set; } = true;

        [Display(Name = "Works On Friday")]
        public bool WorksOnFriday { get; set; } = true;

        [Display(Name = "Works On Saturday")]
        public bool WorksOnSaturday { get; set; } = true;

        [Display(Name = "Works On Sunday")]
        public bool WorksOnSunday { get; set; }

        [Display(Name = "Display Name")]
        public string DisplayName => string.IsNullOrEmpty(Lot) ? Name : $"{Name} ({Lot})";
    }
}

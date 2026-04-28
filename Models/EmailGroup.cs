// Project comment: EmailGroup.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the EmailGroup class used by the IT Form application.
    /// </summary>
    public class EmailGroup
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Group Name is required")]
        [StringLength(50)]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; } = string.Empty; // e.g. Precast Group

        [Required(ErrorMessage = "Email Address is required")]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Group Email")]
        public string GroupEmail { get; set; } = string.Empty; // e.g. ytcallstaff@ytcprecast.com

        public bool IsActive { get; set; } = true;

        // Multi-Company association
        public List<EmailGroupCompany> EmailGroupCompanies { get; set; } = new();
    }
}

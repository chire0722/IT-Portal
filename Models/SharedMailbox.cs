// Project comment: SharedMailbox.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SharedMailbox class used by the IT Form application.
    /// </summary>
    public class SharedMailbox
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mailbox Name is required")]
        [StringLength(50)]
        [Display(Name = "Mailbox Name")]
        public string MailboxName { get; set; } = string.Empty; // e.g. Account Dept

        [Required(ErrorMessage = "Mailbox Address is required")]
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Mailbox Address")]
        public string MailboxAddress { get; set; } = string.Empty; // e.g. account@ytcprecast.com

        public bool IsActive { get; set; } = true;

        // Multi-Company association
        public List<SharedMailboxCompany> SharedMailboxCompanies { get; set; } = new();
    }
}

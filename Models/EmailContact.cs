using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using IT_Form.Attributes;

namespace IT_Form.Models
{
    [Index(nameof(EmailAddress), IsUnique = false)]
    public class EmailContact
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        [EncryptColumn]
        public string EmailAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Department { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

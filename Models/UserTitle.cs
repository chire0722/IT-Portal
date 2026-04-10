using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class UserTitle
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Title { get; set; } = string.Empty; // "IT", "HR", "Manager"

        public ApplicationUser User { get; set; } = null!;
    }
}

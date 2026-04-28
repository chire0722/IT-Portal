// Project comment: UserCompany.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the UserCompany class used by the IT Form application.
    /// </summary>
    public class UserCompany
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string CompanyName { get; set; } = string.Empty; // "Pembinaan Yeng Tong", "YTC Precast"

        public ApplicationUser User { get; set; } = null!;
    }
}

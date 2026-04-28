// Project comment: StandardSoftware.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the StandardSoftware class used by the IT Form application.
    /// </summary>
    public class StandardSoftware : ISoftDeletable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Software Name is required")]
        [StringLength(100)]
        [Display(Name = "Software Name")]
        public string SoftwareName { get; set; } = string.Empty;

        public bool IsMandatory { get; set; }
        public bool IsFree { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [Display(Name = "Has Multiple Versions/Editions?")]
        public bool HasMultipleVersions { get; set; } = false;

        // Child license editions/versions linked to this catalog entry
        public virtual ICollection<SoftwareLicense> Licenses { get; set; } = new List<SoftwareLicense>();

        // ISoftDeletable Implementation
        public bool IsDeleted { get; set; } = false;
        public System.DateTime? DeletedAt { get; set; }
        
        [StringLength(450)]
        public string? DeletedByUserId { get; set; }
    }
}


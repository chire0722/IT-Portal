// Project comment: Department.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the Department class used by the IT Form application.
    /// </summary>
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(100)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Multi-Company association
        public List<DepartmentCompany> DepartmentCompanies { get; set; } = new();
    }
}

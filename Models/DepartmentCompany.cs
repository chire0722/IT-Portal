// Project comment: DepartmentCompany.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the DepartmentCompany class used by the IT Form application.
    /// </summary>
    public class DepartmentCompany
    {
        public int Id { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

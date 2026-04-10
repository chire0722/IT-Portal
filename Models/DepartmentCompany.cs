using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    public class DepartmentCompany
    {
        public int Id { get; set; }

        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

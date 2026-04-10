using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    public class EmailGroupCompany
    {
        public int Id { get; set; }

        public int EmailGroupId { get; set; }
        public EmailGroup EmailGroup { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    public class SharedMailboxCompany
    {
        public int Id { get; set; }

        public int SharedMailboxId { get; set; }
        public SharedMailbox SharedMailbox { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

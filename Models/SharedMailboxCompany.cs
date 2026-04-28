// Project comment: SharedMailboxCompany.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SharedMailboxCompany class used by the IT Form application.
    /// </summary>
    public class SharedMailboxCompany
    {
        public int Id { get; set; }

        public int SharedMailboxId { get; set; }
        public SharedMailbox SharedMailbox { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

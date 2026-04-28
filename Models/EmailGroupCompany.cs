// Project comment: EmailGroupCompany.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the EmailGroupCompany class used by the IT Form application.
    /// </summary>
    public class EmailGroupCompany
    {
        public int Id { get; set; }

        public int EmailGroupId { get; set; }
        public EmailGroup EmailGroup { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

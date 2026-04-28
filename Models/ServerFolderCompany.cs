// Project comment: ServerFolderCompany.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the ServerFolderCompany class used by the IT Form application.
    /// </summary>
    public class ServerFolderCompany
    {
        public int Id { get; set; }

        public int ServerFolderId { get; set; }
        public ServerFolder ServerFolder { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    public class ServerFolderCompany
    {
        public int Id { get; set; }

        public int ServerFolderId { get; set; }
        public ServerFolder ServerFolder { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}

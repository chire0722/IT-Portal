using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IT_Form.Models
{
    public class ServerFolder
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Folder Name is required")]
        [StringLength(100)]
        [Display(Name = "Folder Name")]
        public string FolderName { get; set; } = string.Empty; // e.g. Admin, Safety, QAQC

        public bool IsActive { get; set; } = true;

        // Multi-Company association
        public List<ServerFolderCompany> ServerFolderCompanies { get; set; } = new();
    }
}

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    public class SystemSetting
    {
        [Key]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [StringLength(500)]
        public string Value { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
    }
}

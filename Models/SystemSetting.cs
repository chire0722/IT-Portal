// Project comment: SystemSetting.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SystemSetting class used by the IT Form application.
    /// </summary>
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

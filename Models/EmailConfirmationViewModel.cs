// Project comment: EmailConfirmationViewModel.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Provides view data for the EmailConfirmation screen or form.
    /// </summary>
    public class EmailConfirmationViewModel
    {
        [Required]
        [Display(Name = "To Address(es)")]
        public List<string> To { get; set; } = new();

        [Display(Name = "Cc Address(es)")]
        public List<string>? Cc { get; set; } = new();

        [Required]
        public string Subject { get; set; } = string.Empty;

        // Hidden fields to recreate the email context and route back
        public string BodyHtml { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        
        // These fields are needed if we must regenerate an attachment just-in-time
        public int? RequestId { get; set; }
        public string? EmailType { get; set; }
        public bool HasAttachment { get; set; }
        public string? SignatureImage { get; set; }
    }
}

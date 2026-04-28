// Project comment: RequestSharedMailbox.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the RequestSharedMailbox class used by the IT Form application.
    /// </summary>
    public class RequestSharedMailbox
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        public int SharedMailboxId { get; set; }
        public SharedMailbox SharedMailbox { get; set; } = null!;

        [StringLength(50)]
        public string MailboxName { get; set; } = string.Empty;

        [StringLength(100)]
        public string MailboxAddress { get; set; } = string.Empty;
    }
}

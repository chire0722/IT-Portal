// Project comment: RequestEmailGroup.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the RequestEmailGroup class used by the IT Form application.
    /// </summary>
    public class RequestEmailGroup
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        public int EmailGroupId { get; set; }
        public EmailGroup EmailGroup { get; set; } = null!;

        [StringLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(100)]
        public string GroupEmail { get; set; } = string.Empty;
    }
}

// Project comment: FolderPermission.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the FolderPermission class used by the IT Form application.
    /// </summary>
    public class FolderPermission
    {
        public int Id { get; set; }

        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        [Required, StringLength(100)]
        public string FolderName { get; set; } = string.Empty;

        public bool CanRead { get; set; }

        public bool CanModify { get; set; }
    }
}

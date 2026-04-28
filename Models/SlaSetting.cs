// Project comment: SlaSetting.cs is part of the IT Form application source code.

using System;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SlaSetting class used by the IT Form application.
    /// </summary>
    public class SlaSetting
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string RequestType { get; set; } = string.Empty; // e.g., "Onboarding", "Offboarding"

        [Range(1, 365)]
        public int TargetDays { get; set; } = 3;

        [EmailAddress, StringLength(256)]
        public string? NotifyEmail { get; set; }

        // --- Tier 1 Escalation ---
        [Range(1, 365)]
        public int EscalationDaysTier1 { get; set; } = 3;

        [EmailAddress, StringLength(256)]
        public string? EscalationEmailTier1 { get; set; }

        // --- Tier 2 Escalation ---
        [Range(1, 365)]
        public int EscalationDaysTier2 { get; set; } = 7;

        [EmailAddress, StringLength(256)]
        public string? EscalationEmailTier2 { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

// Project comment: AuditLog.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the AuditLog class used by the IT Form application.
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        /// <summary>Actor identifier captured at command execution time.</summary>
        [StringLength(256)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>When the action occurred (UTC).</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Canonical UTC timestamp for command-level auditing.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>The table/entity that was changed (e.g. OnboardingRequest)</summary>
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Primary key value of the changed record</summary>
        [StringLength(50)]
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Action performed: Created, Updated, Deleted, SignHR, SignManager, SignIT, Approve, Reject</summary>
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Email / username of the user who performed the action</summary>
        [StringLength(256)]
        public string PerformedBy { get; set; } = string.Empty;

        /// <summary>Display name of the actor for quick reading.</summary>
        [StringLength(200)]
        public string? ActorName { get; set; }

        /// <summary>Human-readable description of what changed.</summary>
        [StringLength(1000)]
        public string? Details { get; set; }

        /// <summary>JSON snapshot of changed property values</summary>
        public string? Changes { get; set; }

        /// <summary>Serialized request payload for command-level auditing.</summary>
        public string? Payload { get; set; }

        /// <summary>True when the command completed successfully.</summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>Error message captured when a command fails.</summary>
        [StringLength(4000)]
        public string? ErrorMessage { get; set; }

        /// <summary>Originating IP address of the HTTP request.</summary>
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>Groups logs that occurred within the same database transaction.</summary>
        public Guid? CorrelationId { get; set; }
    }
}

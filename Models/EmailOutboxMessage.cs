using System.ComponentModel.DataAnnotations;
using IT_Form.Models.Enums;

namespace IT_Form.Models;

public sealed class EmailOutboxMessage
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string MessageType { get; set; } = string.Empty;

    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AvailableAfterUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LockedUntilUtc { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    [StringLength(2000)]
    public string? LastError { get; set; }
}

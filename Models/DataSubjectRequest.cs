using System.ComponentModel.DataAnnotations;
using IT_Form.Attributes;

namespace IT_Form.Models;

public enum DataSubjectRequestType
{
    Access = 1,
    Portability = 2,
    Correction = 3,
    Deletion = 4
}

public enum DataSubjectRequestStatus
{
    Submitted = 1,
    IdentityVerificationRequired = 2,
    InReview = 3,
    Approved = 4,
    Rejected = 5,
    Completed = 6,
    Cancelled = 7
}

/// <summary>
/// Stores the minimum case metadata for a controlled data-subject request.
/// The entity is deliberately not a data export or deletion executor.
/// </summary>
public sealed class DataSubjectRequest
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    public string RequestReference { get; set; } = Guid.NewGuid().ToString("N");

    public DataSubjectRequestType RequestType { get; set; }
    public DataSubjectRequestStatus Status { get; set; } = DataSubjectRequestStatus.Submitted;

    [Required, StringLength(450)]
    public string SubjectUserId { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string SubmittedByUserId { get; set; } = string.Empty;

    [StringLength(256)]
    public string? CompanyScope { get; set; }

    [StringLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [StringLength(450)]
    public string? AssignedToUserId { get; set; }

    [StringLength(2000), EncryptColumn]
    public string? DecisionReason { get; set; }

    [StringLength(256)]
    public string? CompletionEvidenceReference { get; set; }

    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    [Required, StringLength(64), ConcurrencyCheck]
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<DataSubjectRequestEvent> Events { get; set; } = new List<DataSubjectRequestEvent>();
}

public sealed class DataSubjectRequestEvent
{
    public int Id { get; set; }
    public int DataSubjectRequestId { get; set; }
    public DataSubjectRequest DataSubjectRequest { get; set; } = null!;

    public DataSubjectRequestStatus FromStatus { get; set; }
    public DataSubjectRequestStatus ToStatus { get; set; }

    [Required, StringLength(450)]
    public string ActorUserId { get; set; } = string.Empty;

    [StringLength(2000), EncryptColumn]
    public string? Reason { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

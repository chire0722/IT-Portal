// Project comment: SignatureLinkExpiry.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Signature;

/// <summary>
/// Defines the SignatureLinkExpiry class used by the IT Form application.
/// </summary>
internal static class SignatureLinkExpiry
{
    public static readonly TimeSpan EmployeeSignatureLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan PdfLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Gets the data required by the GetReferenceDate operation.
    /// </summary>
    public static DateTime GetReferenceDate(DateTime createdAt, DateTime? updatedAt, DateTime? completedAt)
    {
        var referenceDate = createdAt;

        if (updatedAt.HasValue && updatedAt.Value > referenceDate)
        {
            referenceDate = updatedAt.Value;
        }

        if (completedAt.HasValue && completedAt.Value > referenceDate)
        {
            referenceDate = completedAt.Value;
        }

        return referenceDate;
    }

    /// <summary>
    /// Runs the IsExpired operation.
    /// </summary>
    public static bool IsExpired(DateTime createdAt, DateTime? updatedAt, DateTime? completedAt, TimeSpan lifetime, DateTime nowUtc)
        => nowUtc > GetReferenceDate(createdAt, updatedAt, completedAt).Add(lifetime);
}

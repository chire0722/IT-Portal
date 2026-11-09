using System;

namespace IT_Form.Services;

/// <summary>
/// Centralizes fail-closed validation for persisted digital signatures.
/// An unsigned section is valid only when every signature field is empty.
/// Any partial or unversioned signature record fails validation.
/// </summary>
public static class SignatureIntegrityGuard
{
    public static bool IsValid(
        ISignatureIntegrityService integrityService,
        string entityType,
        string entityId,
        string? signerName,
        DateTime? signedAtUtc,
        string? signature,
        string? storedHmac,
        string defaultSignerName = "Unknown")
    {
        ArgumentNullException.ThrowIfNull(integrityService);

        var hasAnySignatureData =
            signedAtUtc.HasValue ||
            !string.IsNullOrWhiteSpace(signature) ||
            !string.IsNullOrWhiteSpace(storedHmac);

        if (!hasAnySignatureData)
        {
            return true;
        }

        if (!signedAtUtc.HasValue ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(storedHmac))
        {
            return false;
        }

        return integrityService.Verify(
            entityType,
            entityId,
            string.IsNullOrWhiteSpace(signerName) ? defaultSignerName : signerName,
            signedAtUtc.Value,
            signature,
            storedHmac);
    }
}

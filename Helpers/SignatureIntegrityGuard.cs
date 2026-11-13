using IT_Form.Services;

namespace IT_Form.Helpers;

/// <summary>
/// Treats a signature as unsigned only when all signature metadata is absent.
/// Any partial metadata or invalid/missing HMAC fails closed.
/// </summary>
public static class SignatureIntegrityGuard
{
    public static bool IsValid(
        ISignatureIntegrityService integrityService,
        string entityType,
        string entityId,
        string? signerName,
        string fallbackSignerName,
        DateTime? signedAt,
        string? signature,
        string? signatureHmac)
    {
        ArgumentNullException.ThrowIfNull(integrityService);

        var hasAnyMetadata = signedAt.HasValue
            || !string.IsNullOrWhiteSpace(signature)
            || !string.IsNullOrWhiteSpace(signatureHmac)
            || !string.IsNullOrWhiteSpace(signerName);

        if (!hasAnyMetadata)
        {
            return true;
        }

        if (!signedAt.HasValue
            || string.IsNullOrWhiteSpace(signature)
            || string.IsNullOrWhiteSpace(signatureHmac))
        {
            return false;
        }

        try
        {
            return integrityService.Verify(
                entityType,
                entityId,
                signerName ?? fallbackSignerName,
                signedAt.Value,
                signature,
                signatureHmac);
        }
        catch
        {
            return false;
        }
    }
}

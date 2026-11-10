// Project comment: EmailConfirmationIntegrity.cs is part of the IT Form application source code.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IT_Form.Models;
using Microsoft.AspNetCore.DataProtection;

namespace IT_Form.Helpers;

/// <summary>
/// Protects the server-generated email confirmation context between the preview and send requests.
/// User-editable recipients and subject remain outside this token by design.
/// </summary>
public sealed class EmailConfirmationIntegrity
{
    private readonly IDataProtector _protector;

    public EmailConfirmationIntegrity(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("IT_Form.Notification.EmailConfirmation.v1");
    }

    public string Create(EmailConfirmationViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var payload = new ProtectedContext(
            model.RequestId,
            model.RequestType,
            model.ConfirmationActorId,
            model.EmailType,
            model.HasAttachment,
            Hash(model.BodyHtml),
            Hash(model.SignatureImage));
        return _protector.Protect(JsonSerializer.Serialize(payload));
    }

    public bool Validate(EmailConfirmationViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        try
        {
            var payload = JsonSerializer.Deserialize<ProtectedContext>(_protector.Unprotect(model.BodyIntegrity));
            return payload is not null &&
                   payload.RequestId == model.RequestId &&
                   string.Equals(payload.RequestType, model.RequestType, StringComparison.Ordinal) &&
                   string.Equals(payload.ConfirmationActorId, model.ConfirmationActorId, StringComparison.Ordinal) &&
                   string.Equals(payload.EmailType, model.EmailType, StringComparison.Ordinal) &&
                   payload.HasAttachment == model.HasAttachment &&
                   FixedEquals(payload.BodyHash, Hash(model.BodyHtml)) &&
                   FixedEquals(payload.SignatureImageHash, Hash(model.SignatureImage));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    private static string Hash(string? value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));

    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left ?? string.Empty),
            Encoding.UTF8.GetBytes(right));

    private sealed record ProtectedContext(
        int? RequestId,
        string? RequestType,
        string? ConfirmationActorId,
        string? EmailType,
        bool HasAttachment,
        string BodyHash,
        string SignatureImageHash);
}


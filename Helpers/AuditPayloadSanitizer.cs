// Project comment: AuditPayloadSanitizer.cs is part of the IT Form application source code.

using System.Text.Json;

namespace IT_Form.Helpers;

/// <summary>
/// Defines the AuditPayloadSanitizer class used by the IT Form application.
/// </summary>
public static class AuditPayloadSanitizer
{
    private const string Redacted = "***REDACTED***";

    private static readonly string[] SensitiveFieldFragments =
    [
        "password",
        "token",
        "signature",
        "hmac",
        "hash",
        "keyvalue",
        "apikey",
        "secret",
        "code",
        "file",
        "image",
        "resignationletter",
        "authorization",
        "cookie",
        "credential",
        "connectionstring",
        "privatekey",
        "clientsecret",
        "accesskey",
        "refresh",
        "otp",
        "emailbody"
    ];

    private static readonly HashSet<string> PersonalFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "FullName",
        "EmployeeID",
        "EmployeeId",
        "UserName",
        "UserId",
        "CreatedByUserId",
        "Email",
        "EmailAddress",
        "HODEmail",
        "ContactNumber",
        "PhoneNumber",
        "Address",
        "IpAddress",
        "ForwardToEmail",
        "EmployeeSignerName",
        "RequesterSignerName",
        "HRSignerName",
        "HODSignerName",
        "ITAdminSignerName",
        "RejectionReason"
    };

    /// <summary>
    /// Runs the SerializeSanitized operation.
    /// </summary>
    public static string SerializeSanitized(object value, JsonSerializerOptions options)
    {
        using var document = JsonSerializer.SerializeToDocument(value, value.GetType(), options);
        return JsonSerializer.Serialize(RedactElement(document.RootElement), options);
    }

    /// <summary>
    /// Runs the SanitizeJson operation.
    /// </summary>
    public static string SanitizeJson(string? json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(RedactElement(document.RootElement), options);
        }
        catch (JsonException)
        {
            return RedactString(json) ?? string.Empty;
        }
    }

    /// <summary>
    /// Runs the RedactElement operation.
    /// </summary>
    private static object? RedactElement(JsonElement element, string? propertyName = null)
    {
        if (IsSensitiveField(propertyName))
        {
            return Redacted;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => RedactElement(property.Value, property.Name),
                    StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => RedactElement(item))
                .ToList(),
            JsonValueKind.String => RedactString(element.GetString()),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Runs the IsSensitiveField operation.
    /// </summary>
    private static bool IsSensitiveField(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        if (PersonalFieldNames.Contains(propertyName))
        {
            return true;
        }

        return SensitiveFieldFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Runs the RedactString operation.
    /// </summary>
    private static string? RedactString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            value.Length > 2_000)
        {
            return Redacted;
        }

        return value;
    }
}

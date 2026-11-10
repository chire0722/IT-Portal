// Project comment: AuditPayloadSanitizer.cs is part of the IT Form application source code.

using System.Text.Json;
using System.Text.RegularExpressions;

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
        "resignationletter"
    ];

    private static readonly HashSet<string> PersonalFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "FullName",
        "EmployeeID",
        "Email",
        "EmailAddress",
        "HODEmail",
        "ContactNumber",
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
    /// Produces a bounded, non-sensitive value suitable for an audit error field.
    /// Exception messages are not an audit trail and may contain user input or secrets.
    /// </summary>
    public static string SanitizeExceptionMessage(Exception exception) =>
        $"Operation failed ({exception.GetType().Name}).";

    /// <summary>
    /// Redacts common secret and email patterns from non-JSON audit text.
    /// </summary>
    public static string RedactFreeText(string? value)
    {
        var bounded = RedactString(value) ?? string.Empty;
        bounded = Regex.Replace(
            bounded,
            @"(?i)\b(password|token|secret|api[-_]?key|hmac|signature|authorization|bearer)\s*[:=]\s*[^\s,;]+",
            "$1=***REDACTED***");
        return Regex.Replace(
            bounded,
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            Redacted,
            RegexOptions.IgnoreCase);
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

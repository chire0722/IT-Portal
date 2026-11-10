// Project comment: RequestLogPathSanitizer.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Http;

namespace IT_Form.Security;

/// <summary>
/// Removes bearer tokens and employee identifiers from URL paths before request logging.
/// Query strings are not included in Serilog's RequestPath property.
/// </summary>
public static class RequestLogPathSanitizer
{
    private static readonly (string Prefix, string SafePath)[] SensitiveRoutePrefixes =
    [
        ("/verify/", "/verify/[REDACTED]"),
        ("/api/inventory/status/", "/api/inventory/status/[REDACTED]"),
        ("/api/inventory/download/", "/api/inventory/download/[REDACTED]"),
        ("/api/inventory/generate-token/", "/api/inventory/generate-token/[REDACTED]")
    ];

    public static string Sanitize(PathString requestPath)
    {
        var path = requestPath.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        foreach (var (prefix, safePath) in SensitiveRoutePrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return safePath;
            }
        }

        var segments = path.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (Guid.TryParse(segments[index], out _) || IsOpaqueToken(segments[index]))
            {
                segments[index] = "[REDACTED]";
            }
        }

        return string.Join("/", segments);
    }

    private static bool IsOpaqueToken(string segment)
    {
        if (segment.Length < 32)
        {
            return false;
        }

        return segment.All(character =>
            char.IsLetterOrDigit(character) ||
            character is '-' or '_' or '=');
    }
}

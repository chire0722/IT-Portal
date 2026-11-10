// Project comment: SecurityHeadersMiddleware.cs is part of the IT Form application source code.

using System.Security.Cryptography;

namespace IT_Form.Security
{
    /// <summary>
    /// Defines the SecurityHeadersMiddleware class used by the IT Form application.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        public const string CspNonceItemKey = "IT_Form.Security.CspNonce";

        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

        /// <summary>
        /// Runs the InvokeAsync operation.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Content Security Policy (CSP)
            // Inline script blocks receive a per-response nonce through CspNonceTagHelper.
            // Inline event attributes remain temporarily compatible through script-src-attr
            // while the existing Razor views are migrated to unobtrusive handlers.
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[CspNonceItemKey] = nonce;
            var isVerificationPage = context.Request.Path.StartsWithSegments(
                "/Verification", StringComparison.OrdinalIgnoreCase);
            var scriptSources = isVerificationPage
                ? $"script-src 'self' 'nonce-{nonce}' 'unsafe-eval' https://cdn.tailwindcss.com https://cdn.jsdelivr.net; "
                : $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; ";

            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                scriptSources +
                "script-src-attr 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
                "font-src 'self' data: https://cdnjs.cloudflare.com https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
                "img-src 'self' data: blob:; " +
                "connect-src 'self'; " +
                "media-src 'self' blob:; " +
                "worker-src 'self' blob:; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'none';");

            // 2. Anti-Clickjacking: X-Frame-Options
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // 3. MIME Sniffing Protection: X-Content-Type-Options
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // 4. Referrer Policy
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // 5. Strict-Transport-Security (HSTS) - Disabled for local IP access to prevent lockout on HTTP
            // context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            // 6. X-XSS-Protection (Legacy protection for older browsers)
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // 7. Permissions Policy (Optional, but good practice)
            context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            await _next(context);
        }
    }
}


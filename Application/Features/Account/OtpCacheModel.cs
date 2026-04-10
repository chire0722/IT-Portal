using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace IT_Form.Application.Features.Account;

public sealed class OtpCacheModel
{
    public required string Code { get; init; }
    public int FailedAttempts { get; set; }
}

public static class AccountOtpSupport
{
    public const int OtpLength = 6;
    public const int MaxFailedAttempts = 3;
    public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(1);

    public static string BuildOtpCacheKey(string userId) => $"OTP_{userId}";

    public static string BuildCooldownCacheKey(string userId) => $"OTP_COOLDOWN_{userId}";

    public static string BuildOperationKey(string userId) => $"OTP_LOCK_{userId}";

    public static MemoryCacheEntryOptions BuildOtpEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = OtpLifetime
    };

    public static MemoryCacheEntryOptions BuildCooldownEntryOptions(DateTimeOffset availableAtUtc) => new()
    {
        AbsoluteExpiration = availableAtUtc
    };

    public static ValidationException CreateValidationException(string message)
        => new([new ValidationFailure(string.Empty, message)]);

    public static string NormalizeCode(string? code)
        => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static string HashCode(string code)
    {
        var normalizedCode = NormalizeCode(code);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode));
        return $"sha256:{Convert.ToBase64String(bytes)}";
    }

    public static bool MatchesStoredCode(string providedCode, string storedCode)
    {
        if (storedCode.StartsWith("sha256:", StringComparison.Ordinal))
        {
            var expectedBytes = Encoding.UTF8.GetBytes(storedCode);
            var providedBytes = Encoding.UTF8.GetBytes(HashCode(providedCode));
            return expectedBytes.Length == providedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }

        var normalizedExpectedCode = Encoding.UTF8.GetBytes(NormalizeCode(storedCode));
        var normalizedProvidedCode = Encoding.UTF8.GetBytes(NormalizeCode(providedCode));
        return normalizedExpectedCode.Length == normalizedProvidedCode.Length &&
               CryptographicOperations.FixedTimeEquals(normalizedExpectedCode, normalizedProvidedCode);
    }

    public static string BuildOtpEmailSubject()
        => "IT Form - Password Recovery Code";

    public static string BuildOtpEmailBody(string code) => $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                <h2 style='color: #2563eb;'>Password Recovery</h2>
                <p>You requested a password reset. Please use the following code to verify your identity:</p>
                <div style='background: #f3f4f6; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111; margin: 20px 0;'>
                    {code}
                </div>
                <p style='color: #666; font-size: 14px;'>This code is for <b>one-time use</b> and will expire in <b>5 minutes</b>. If you did not request this, please ignore this email.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                <small>IT Asset Management System</small>
            </div>";
}

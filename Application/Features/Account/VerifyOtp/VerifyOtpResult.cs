// Project comment: VerifyOtpResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    /// <summary>
    /// Represents the result returned by the VerifyOtp workflow.
    /// </summary>
    public sealed record VerifyOtpResult(
        bool IsVerified,
        bool ShouldRedirectToLogin,
        string Message,
        string? Token = null,
        bool IsLoginVerified = false,
        bool RequiresPasswordSetup = false);
}

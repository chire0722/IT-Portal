namespace IT_Form.Application.Features.Account.VerifyOtp
{
    public sealed record VerifyOtpResult(
        bool IsVerified,
        bool ShouldRedirectToLogin,
        string Message,
        string? Token = null);
}

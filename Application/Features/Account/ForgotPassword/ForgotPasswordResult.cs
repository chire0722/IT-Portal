namespace IT_Form.Application.Features.Account.ForgotPassword
{
    public sealed record ForgotPasswordResult(
        bool OtpSent,
        string Message,
        string? Email = null);
}

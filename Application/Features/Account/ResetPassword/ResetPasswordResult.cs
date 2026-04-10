namespace IT_Form.Application.Features.Account.ResetPassword
{
    public sealed record ResetPasswordResult(
        bool Succeeded,
        bool ShouldRedirectToLogin,
        IReadOnlyList<string> Errors);
}

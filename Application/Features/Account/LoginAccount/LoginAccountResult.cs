namespace IT_Form.Application.Features.Account.LoginAccount
{
    public sealed record LoginAccountResult(
        bool IsAuthenticated,
        string Message,
        bool MustChangePassword = false,
        DateTimeOffset? LockoutEndUtc = null);
}

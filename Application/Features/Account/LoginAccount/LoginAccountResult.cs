// Project comment: LoginAccountResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Account.LoginAccount
{
    /// <summary>
    /// Represents the result returned by the LoginAccount workflow.
    /// </summary>
    public sealed record LoginAccountResult(
        bool IsAuthenticated,
        string Message,
        bool MustChangePassword = false,
        DateTimeOffset? LockoutEndUtc = null);
}

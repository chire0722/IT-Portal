// Project comment: ResetPasswordResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Account.ResetPassword
{
    /// <summary>
    /// Represents the result returned by the ResetPassword workflow.
    /// </summary>
    public sealed record ResetPasswordResult(
        bool Succeeded,
        bool ShouldRedirectToLogin,
        IReadOnlyList<string> Errors);
}

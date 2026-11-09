// Project comment: ForgotPasswordResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Account.ForgotPassword
{
    /// <summary>
    /// Represents the result returned by the ForgotPassword workflow.
    /// </summary>
    public sealed record ForgotPasswordResult(
        bool OtpSent,
        string Message,
        string? Email = null)
    {
        public const string GenericResponseMessage =
            "If an eligible account exists, a verification code will be sent to the registered email address.";
    }
}

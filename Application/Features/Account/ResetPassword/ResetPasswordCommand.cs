// Project comment: ResetPasswordCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.ResetPassword
{
    /// <summary>
    /// Carries data needed to execute the ResetPassword command.
    /// </summary>
    public sealed record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword) : IRequest<ResetPasswordResult>;
}

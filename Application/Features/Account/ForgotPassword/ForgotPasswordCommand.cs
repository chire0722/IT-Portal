// Project comment: ForgotPasswordCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.ForgotPassword
{
    /// <summary>
    /// Carries data needed to execute the ForgotPassword command.
    /// </summary>
    public sealed record ForgotPasswordCommand(
        string Username,
        string ClientIp) : IRequest<ForgotPasswordResult>;
}

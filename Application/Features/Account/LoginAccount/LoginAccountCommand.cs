// Project comment: LoginAccountCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.LoginAccount
{
    /// <summary>
    /// Carries data needed to execute the LoginAccount command.
    /// </summary>
    public sealed record LoginAccountCommand(
        string Username,
        string Password,
        bool RememberMe,
        string ClientIp) : IRequest<LoginAccountResult>;
}

using MediatR;

namespace IT_Form.Application.Features.Account.LoginAccount
{
    public sealed record LoginAccountCommand(
        string Username,
        string Password,
        bool RememberMe,
        string ClientIp) : IRequest<LoginAccountResult>;
}

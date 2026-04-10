using MediatR;

namespace IT_Form.Application.Features.Account.ForgotPassword
{
    public sealed record ForgotPasswordCommand(
        string Username,
        string ClientIp) : IRequest<ForgotPasswordResult>;
}

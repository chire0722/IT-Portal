using MediatR;

namespace IT_Form.Application.Features.Account.ResetPassword
{
    public sealed record ResetPasswordCommand(
        string Email,
        string Token,
        string NewPassword) : IRequest<ResetPasswordResult>;
}

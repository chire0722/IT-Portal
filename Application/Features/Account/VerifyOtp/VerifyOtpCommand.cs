using MediatR;

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    public sealed record VerifyOtpCommand(
        string Email,
        string Code) : IRequest<VerifyOtpResult>;
}

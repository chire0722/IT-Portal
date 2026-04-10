using MediatR;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

public sealed record ResendOtpCommand(
    string Email,
    string ClientIp) : IRequest<ResendOtpResult>;

public sealed record ResendOtpResult(
    string Email,
    bool IsQueued,
    string Message);

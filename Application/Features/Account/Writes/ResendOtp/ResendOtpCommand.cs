// Project comment: ResendOtpCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

/// <summary>
/// Carries data needed to execute the ResendOtp command.
/// </summary>
public sealed record ResendOtpCommand(
    string Email,
    string ClientIp) : IRequest<ResendOtpResult>;

/// <summary>
/// Represents the result returned by the ResendOtp workflow.
/// </summary>
public sealed record ResendOtpResult(
    string Email,
    bool IsQueued,
    string Message);

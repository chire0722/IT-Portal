// Project comment: ResendOtpCommand.cs is part of the IT Form application source code.

using MediatR;

using IT_Form.Application.Features.Account;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

/// <summary>
/// Carries data needed to execute the ResendOtp command.
/// </summary>
public sealed record ResendOtpCommand(
    string Email,
    string ClientIp,
    string Purpose = OtpPurposes.PasswordReset) : IRequest<ResendOtpResult>;

/// <summary>
/// Represents the result returned by the ResendOtp workflow.
/// </summary>
public sealed record ResendOtpResult(
    string Email,
    bool IsQueued,
    string Message)
{
    public const string GenericResponseMessage =
        "If an eligible account exists, a verification code will be sent to its registered email address.";
}

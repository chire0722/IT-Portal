// Project comment: VerifyOtpCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    /// <summary>
    /// Command that verifies a one-time password code for the supplied email address.
    /// </summary>
    /// <param name="Email">Email address that requested or owns the OTP code.</param>
    /// <param name="Code">One-time password code entered by the user.</param>
    public sealed record VerifyOtpCommand(
        string Email,
        string Code) : IRequest<VerifyOtpResult>;
}

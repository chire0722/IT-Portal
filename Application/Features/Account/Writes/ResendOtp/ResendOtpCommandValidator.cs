// Project comment: ResendOtpCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

/// <summary>
/// Validates input for the ResendOtpCommand request.
/// </summary>
public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.ClientIp)
            .NotEmpty();

        RuleFor(x => x.Purpose)
            .Must(purpose => purpose == OtpPurposes.PasswordReset)
            .WithMessage("Unsupported OTP purpose for resend.");
    }
}

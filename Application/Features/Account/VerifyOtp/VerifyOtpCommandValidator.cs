// Project comment: VerifyOtpCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    /// <summary>
    /// Validates input for the VerifyOtpCommand request.
    /// </summary>
    public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
    {
        public VerifyOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.");

            RuleFor(x => x.Purpose)
                .Must(purpose => purpose == OtpPurposes.PasswordReset || purpose == OtpPurposes.PrivilegedLogin)
                .WithMessage("Unsupported OTP purpose.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("OTP code is required.")
                .Length(6)
                .WithMessage("OTP code must be exactly 6 characters.")
                .Matches("^[A-HJ-NP-Za-hj-np-z2-9]{6}$")
                .WithMessage("OTP code must contain only letters and numbers.");
        }
    }
}

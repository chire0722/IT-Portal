using FluentValidation;

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
    {
        public VerifyOtpCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.");

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

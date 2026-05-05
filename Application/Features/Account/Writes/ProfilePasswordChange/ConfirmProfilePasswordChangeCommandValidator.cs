// Project comment: ConfirmProfilePasswordChangeCommandValidator.cs validates profile password-change OTP confirmation.

using FluentValidation;

namespace IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;

public sealed class ConfirmProfilePasswordChangeCommandValidator : AbstractValidator<ConfirmProfilePasswordChangeCommand>
{
    public ConfirmProfilePasswordChangeCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Code)
            .NotEmpty()
            .WithMessage("Verification code is required.")
            .Length(AccountOtpSupport.OtpLength)
            .WithMessage($"Verification code must be {AccountOtpSupport.OtpLength} digits.");
    }
}

// Project comment: SignOffHrOnboardingCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    /// <summary>
    /// Validates input for the SignOffHrOnboardingCommand request.
    /// </summary>
    public sealed class SignOffHrOnboardingCommandValidator : AbstractValidator<SignOffHrOnboardingCommand>
    {
        public SignOffHrOnboardingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureBase64)
                .NotEmpty()
                .MaximumLength(500_000)
                .WithMessage("Signature data is invalid or too large. Please try again.");
        }
    }
}


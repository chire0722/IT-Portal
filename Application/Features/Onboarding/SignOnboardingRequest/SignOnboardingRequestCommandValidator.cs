// Project comment: SignOnboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    /// <summary>
    /// Validates input for the SignOnboardingRequestCommand request.
    /// </summary>
    public sealed class SignOnboardingRequestCommandValidator : AbstractValidator<SignOnboardingRequestCommand>
    {
        public SignOnboardingRequestCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureType)
                .NotEmpty();
            RuleFor(x => x.SignatureData)
                .NotEmpty()
                .MaximumLength(500_000)
                .WithMessage("Signature data is invalid or too large.");
        }
    }
}

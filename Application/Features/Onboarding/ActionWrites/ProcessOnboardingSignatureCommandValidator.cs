// Project comment: ProcessOnboardingSignatureCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Validates input for the ProcessOnboardingSignatureCommand request.
    /// </summary>
    public sealed class ProcessOnboardingSignatureCommandValidator : AbstractValidator<ProcessOnboardingSignatureCommand>
    {
        public ProcessOnboardingSignatureCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.SignatureType).NotEmpty();
            RuleFor(x => x.SignatureData)
                .NotEmpty()
                .MaximumLength(500_000)
                .WithMessage("Signature data is invalid or too large.");
        }
    }
}

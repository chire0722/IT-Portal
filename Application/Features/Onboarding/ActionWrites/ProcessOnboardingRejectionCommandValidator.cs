// Project comment: ProcessOnboardingRejectionCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Validates input for the ProcessOnboardingRejectionCommand request.
    /// </summary>
    public sealed class ProcessOnboardingRejectionCommandValidator : AbstractValidator<ProcessOnboardingRejectionCommand>
    {
        public ProcessOnboardingRejectionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("A rejection reason is required.");
        }
    }
}

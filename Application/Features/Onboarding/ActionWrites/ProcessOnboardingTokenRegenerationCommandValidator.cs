// Project comment: ProcessOnboardingTokenRegenerationCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    /// <summary>
    /// Validates input for the ProcessOnboardingTokenRegenerationCommand request.
    /// </summary>
    public sealed class ProcessOnboardingTokenRegenerationCommandValidator : AbstractValidator<ProcessOnboardingTokenRegenerationCommand>
    {
        public ProcessOnboardingTokenRegenerationCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed class ProcessOnboardingTokenRegenerationCommandValidator : AbstractValidator<ProcessOnboardingTokenRegenerationCommand>
    {
        public ProcessOnboardingTokenRegenerationCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    public sealed class RegenerateOnboardingTokenCommandValidator : AbstractValidator<RegenerateOnboardingTokenCommand>
    {
        public RegenerateOnboardingTokenCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

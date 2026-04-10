using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    public sealed class RegenerateOffboardingTokenCommandValidator : AbstractValidator<RegenerateOffboardingTokenCommand>
    {
        public RegenerateOffboardingTokenCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

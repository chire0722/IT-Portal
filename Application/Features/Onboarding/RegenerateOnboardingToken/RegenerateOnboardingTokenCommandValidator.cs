// Project comment: RegenerateOnboardingTokenCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    /// <summary>
    /// Validates input for the RegenerateOnboardingTokenCommand request.
    /// </summary>
    public sealed class RegenerateOnboardingTokenCommandValidator : AbstractValidator<RegenerateOnboardingTokenCommand>
    {
        public RegenerateOnboardingTokenCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

// Project comment: RegenerateOffboardingTokenCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    /// <summary>
    /// Validates input for the RegenerateOffboardingTokenCommand request.
    /// </summary>
    public sealed class RegenerateOffboardingTokenCommandValidator : AbstractValidator<RegenerateOffboardingTokenCommand>
    {
        public RegenerateOffboardingTokenCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

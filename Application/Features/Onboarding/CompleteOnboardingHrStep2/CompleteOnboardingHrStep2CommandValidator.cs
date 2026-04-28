// Project comment: CompleteOnboardingHrStep2CommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    /// <summary>
    /// Validates input for the CompleteOnboardingHrStep2Command request.
    /// </summary>
    public sealed class CompleteOnboardingHrStep2CommandValidator : AbstractValidator<CompleteOnboardingHrStep2Command>
    {
        public CompleteOnboardingHrStep2CommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.Session).NotNull();
        }
    }
}

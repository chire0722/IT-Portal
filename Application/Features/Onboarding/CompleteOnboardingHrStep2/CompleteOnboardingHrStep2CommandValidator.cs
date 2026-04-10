using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    public sealed class CompleteOnboardingHrStep2CommandValidator : AbstractValidator<CompleteOnboardingHrStep2Command>
    {
        public CompleteOnboardingHrStep2CommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.Session).NotNull();
        }
    }
}

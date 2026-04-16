using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    public sealed class CompleteOnboardingItSetupCommandValidator : AbstractValidator<CompleteOnboardingItSetupCommand>
    {
        public CompleteOnboardingItSetupCommandValidator()
        {
            RuleFor(x => x.Session)
                .NotNull()
                .DependentRules(() =>
                {
                    RuleFor(x => x.Session!.Id).GreaterThan(0);
                });

            RuleFor(x => x.ChecklistRemarks).MaximumLength(2000);
        }
    }
}

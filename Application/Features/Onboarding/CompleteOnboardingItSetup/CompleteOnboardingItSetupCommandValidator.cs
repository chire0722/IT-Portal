// Project comment: CompleteOnboardingItSetupCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    /// <summary>
    /// Validates the IT setup completion command before the handler applies checklist changes.
    /// </summary>
    public sealed class CompleteOnboardingItSetupCommandValidator : AbstractValidator<CompleteOnboardingItSetupCommand>
    {
        /// <summary>
        /// Configures the required session and checklist remark length rules.
        /// </summary>
        public CompleteOnboardingItSetupCommandValidator()
        {
            // The session supplies the onboarding request id that all updates are applied to.
            RuleFor(x => x.Session)
                .NotNull()
                .DependentRules(() =>
                {
                    RuleFor(x => x.Session!.Id).GreaterThan(0);
                });

            // Keep remarks short enough for the backing model and UI display.
            RuleFor(x => x.ChecklistRemarks).MaximumLength(2000);
        }
    }
}

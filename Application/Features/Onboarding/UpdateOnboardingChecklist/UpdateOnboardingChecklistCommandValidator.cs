using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    public sealed class UpdateOnboardingChecklistCommandValidator : AbstractValidator<UpdateOnboardingChecklistCommand>
    {
        public UpdateOnboardingChecklistCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ChecklistRemarks)
                .MaximumLength(2000);
        }
    }
}

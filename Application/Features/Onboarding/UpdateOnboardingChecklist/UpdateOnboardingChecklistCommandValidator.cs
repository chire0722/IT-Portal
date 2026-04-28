// Project comment: UpdateOnboardingChecklistCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    /// <summary>
    /// Validates input for the UpdateOnboardingChecklistCommand request.
    /// </summary>
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

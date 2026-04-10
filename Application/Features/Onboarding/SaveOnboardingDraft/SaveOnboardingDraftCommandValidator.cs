using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    public sealed class SaveOnboardingDraftCommandValidator : AbstractValidator<SaveOnboardingDraftCommand>
    {
        public SaveOnboardingDraftCommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
        }
    }
}


// Project comment: SaveOnboardingDraftCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    /// <summary>
    /// Validates input for the SaveOnboardingDraftCommand request.
    /// </summary>
    public sealed class SaveOnboardingDraftCommandValidator : AbstractValidator<SaveOnboardingDraftCommand>
    {
        public SaveOnboardingDraftCommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
        }
    }
}


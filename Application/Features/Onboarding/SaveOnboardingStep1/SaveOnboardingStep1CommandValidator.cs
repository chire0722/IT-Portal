using FluentValidation;
using IT_Form.Services;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    public sealed class SaveOnboardingStep1CommandValidator : AbstractValidator<SaveOnboardingStep1Command>
    {
        public SaveOnboardingStep1CommandValidator(IOnboardingHodService onboardingHodService)
        {
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.Session).NotNull();
            RuleFor(x => x.Model)
                .MustAsync(async (model, cancellationToken) =>
                {
                    if (model == null ||
                        string.IsNullOrWhiteSpace(model.Branch) ||
                        string.IsNullOrWhiteSpace(model.HODEmail))
                    {
                        return true;
                    }

                    return await onboardingHodService.IsValidHodForCompanyAsync(model.Branch, model.HODEmail);
                })
                .WithName("HODEmail")
                .WithMessage("Selected HOD is not assigned to the selected company.");
        }
    }
}

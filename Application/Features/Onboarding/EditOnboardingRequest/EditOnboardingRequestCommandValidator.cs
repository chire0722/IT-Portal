// Project comment: EditOnboardingRequestCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Services;

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    /// <summary>
    /// Validates input for the EditOnboardingRequestCommand request.
    /// </summary>
    public sealed class EditOnboardingRequestCommandValidator : AbstractValidator<EditOnboardingRequestCommand>
    {
        public EditOnboardingRequestCommandValidator(IOnboardingHodService onboardingHodService)
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.ActionType).NotEmpty();
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

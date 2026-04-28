// Project comment: UpdateOnboardingItStep4CommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    /// <summary>
    /// Validates input for the UpdateOnboardingItStep4Command request.
    /// </summary>
    public sealed class UpdateOnboardingItStep4CommandValidator : AbstractValidator<UpdateOnboardingItStep4Command>
    {
        public UpdateOnboardingItStep4CommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.Session).NotNull();
            RuleFor(x => x.Session.Id).GreaterThan(0);
        }
    }
}

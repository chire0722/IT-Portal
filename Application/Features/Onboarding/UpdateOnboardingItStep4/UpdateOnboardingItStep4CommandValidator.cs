using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
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

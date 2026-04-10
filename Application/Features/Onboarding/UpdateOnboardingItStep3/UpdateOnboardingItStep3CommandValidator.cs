using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    public sealed class UpdateOnboardingItStep3CommandValidator : AbstractValidator<UpdateOnboardingItStep3Command>
    {
        public UpdateOnboardingItStep3CommandValidator()
        {
            RuleFor(x => x.Model).NotNull();
            RuleFor(x => x.Session).NotNull();
            RuleFor(x => x.Session.Id).GreaterThan(0);
        }
    }
}

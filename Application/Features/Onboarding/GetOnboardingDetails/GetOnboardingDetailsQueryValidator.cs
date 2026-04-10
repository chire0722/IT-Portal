using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    public sealed class GetOnboardingDetailsQueryValidator : AbstractValidator<GetOnboardingDetailsQuery>
    {
        public GetOnboardingDetailsQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

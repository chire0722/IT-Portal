using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    public sealed class GetOnboardingEditQueryValidator : AbstractValidator<GetOnboardingEditQuery>
    {
        public GetOnboardingEditQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

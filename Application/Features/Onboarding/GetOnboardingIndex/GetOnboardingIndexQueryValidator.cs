using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    public sealed class GetOnboardingIndexQueryValidator : AbstractValidator<GetOnboardingIndexQuery>
    {
        public GetOnboardingIndexQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
        }
    }
}

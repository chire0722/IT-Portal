// Project comment: GetOnboardingIndexQueryValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    /// <summary>
    /// Validates input for the GetOnboardingIndexQuery request.
    /// </summary>
    public sealed class GetOnboardingIndexQueryValidator : AbstractValidator<GetOnboardingIndexQuery>
    {
        public GetOnboardingIndexQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0);
        }
    }
}

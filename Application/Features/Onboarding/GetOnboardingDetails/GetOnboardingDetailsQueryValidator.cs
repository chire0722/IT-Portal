// Project comment: GetOnboardingDetailsQueryValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    /// <summary>
    /// Validates input for the GetOnboardingDetailsQuery request.
    /// </summary>
    public sealed class GetOnboardingDetailsQueryValidator : AbstractValidator<GetOnboardingDetailsQuery>
    {
        public GetOnboardingDetailsQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

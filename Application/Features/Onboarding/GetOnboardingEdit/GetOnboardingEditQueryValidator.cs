// Project comment: GetOnboardingEditQueryValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    /// <summary>
    /// Validates input for the GetOnboardingEditQuery request.
    /// </summary>
    public sealed class GetOnboardingEditQueryValidator : AbstractValidator<GetOnboardingEditQuery>
    {
        public GetOnboardingEditQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

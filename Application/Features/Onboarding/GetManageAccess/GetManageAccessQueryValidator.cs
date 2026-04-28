// Project comment: GetManageAccessQueryValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetManageAccess
{
    /// <summary>
    /// Validates input for the GetManageAccessQuery request.
    /// </summary>
    public sealed class GetManageAccessQueryValidator : AbstractValidator<GetManageAccessQuery>
    {
        public GetManageAccessQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

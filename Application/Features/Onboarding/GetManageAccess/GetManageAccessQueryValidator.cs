using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.GetManageAccess
{
    public sealed class GetManageAccessQueryValidator : AbstractValidator<GetManageAccessQuery>
    {
        public GetManageAccessQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}

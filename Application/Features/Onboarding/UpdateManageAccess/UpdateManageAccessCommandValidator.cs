using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    public sealed class UpdateManageAccessCommandValidator : AbstractValidator<UpdateManageAccessCommand>
    {
        public UpdateManageAccessCommandValidator()
        {
            RuleFor(x => x.OnboardingId)
                .GreaterThan(0);
        }
    }
}

using FluentValidation;

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    public sealed class AddCurrentEmployeeToEmailGroupCommandValidator : AbstractValidator<AddCurrentEmployeeToEmailGroupCommand>
    {
        public AddCurrentEmployeeToEmailGroupCommandValidator()
        {
            RuleFor(x => x.EmailGroupId).GreaterThan(0);
            RuleFor(x => x.OnboardingRequestId).GreaterThan(0);
        }
    }
}

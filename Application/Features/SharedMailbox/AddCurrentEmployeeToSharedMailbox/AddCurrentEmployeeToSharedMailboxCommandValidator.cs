using FluentValidation;

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    public sealed class AddCurrentEmployeeToSharedMailboxCommandValidator : AbstractValidator<AddCurrentEmployeeToSharedMailboxCommand>
    {
        public AddCurrentEmployeeToSharedMailboxCommandValidator()
        {
            RuleFor(x => x.SharedMailboxId).GreaterThan(0);
            RuleFor(x => x.OnboardingRequestId).GreaterThan(0);
        }
    }
}

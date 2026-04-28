// Project comment: AddCurrentEmployeeToSharedMailboxCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    /// <summary>
    /// Validates input for the AddCurrentEmployeeToSharedMailboxCommand request.
    /// </summary>
    public sealed class AddCurrentEmployeeToSharedMailboxCommandValidator : AbstractValidator<AddCurrentEmployeeToSharedMailboxCommand>
    {
        public AddCurrentEmployeeToSharedMailboxCommandValidator()
        {
            RuleFor(x => x.SharedMailboxId).GreaterThan(0);
            RuleFor(x => x.OnboardingRequestId).GreaterThan(0);
        }
    }
}

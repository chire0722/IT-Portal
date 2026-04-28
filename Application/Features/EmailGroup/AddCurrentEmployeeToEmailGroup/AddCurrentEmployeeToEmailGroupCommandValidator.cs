// Project comment: AddCurrentEmployeeToEmailGroupCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    /// <summary>
    /// Validates input for the AddCurrentEmployeeToEmailGroupCommand request.
    /// </summary>
    public sealed class AddCurrentEmployeeToEmailGroupCommandValidator : AbstractValidator<AddCurrentEmployeeToEmailGroupCommand>
    {
        public AddCurrentEmployeeToEmailGroupCommandValidator()
        {
            RuleFor(x => x.EmailGroupId).GreaterThan(0);
            RuleFor(x => x.OnboardingRequestId).GreaterThan(0);
        }
    }
}

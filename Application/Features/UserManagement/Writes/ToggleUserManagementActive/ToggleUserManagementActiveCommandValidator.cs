using FluentValidation;

namespace IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;

public sealed class ToggleUserManagementActiveCommandValidator : AbstractValidator<ToggleUserManagementActiveCommand>
{
    public ToggleUserManagementActiveCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

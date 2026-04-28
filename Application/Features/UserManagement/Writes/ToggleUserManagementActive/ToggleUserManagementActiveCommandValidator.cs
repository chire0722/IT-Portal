// Project comment: ToggleUserManagementActiveCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;

/// <summary>
/// Validates input for the ToggleUserManagementActiveCommand request.
/// </summary>
public sealed class ToggleUserManagementActiveCommandValidator : AbstractValidator<ToggleUserManagementActiveCommand>
{
    public ToggleUserManagementActiveCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

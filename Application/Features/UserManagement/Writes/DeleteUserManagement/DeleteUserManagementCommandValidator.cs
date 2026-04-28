// Project comment: DeleteUserManagementCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;

/// <summary>
/// Validates input for the DeleteUserManagementCommand request.
/// </summary>
public sealed class DeleteUserManagementCommandValidator : AbstractValidator<DeleteUserManagementCommand>
{
    public DeleteUserManagementCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

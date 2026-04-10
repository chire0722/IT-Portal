using FluentValidation;

namespace IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;

public sealed class DeleteUserManagementCommandValidator : AbstractValidator<DeleteUserManagementCommand>
{
    public DeleteUserManagementCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

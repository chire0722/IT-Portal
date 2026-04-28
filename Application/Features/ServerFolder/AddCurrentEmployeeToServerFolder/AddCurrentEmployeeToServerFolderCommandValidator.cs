// Project comment: AddCurrentEmployeeToServerFolderCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    /// <summary>
    /// Validates input for the AddCurrentEmployeeToServerFolderCommand request.
    /// </summary>
    public sealed class AddCurrentEmployeeToServerFolderCommandValidator : AbstractValidator<AddCurrentEmployeeToServerFolderCommand>
    {
        public AddCurrentEmployeeToServerFolderCommandValidator()
        {
            RuleFor(x => x.ServerFolderId).GreaterThan(0);
            RuleFor(x => x.OnboardingRequestId).GreaterThan(0);
        }
    }
}

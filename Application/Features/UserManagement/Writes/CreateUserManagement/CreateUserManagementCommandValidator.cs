// Project comment: CreateUserManagementCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Models;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

/// <summary>
/// Validates input for the CreateUserManagementCommand request.
/// </summary>
public sealed class CreateUserManagementCommandValidator : AbstractValidator<CreateUserManagementCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserManagementCommandValidator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(BeUniqueUsernameAsync)
            .WithMessage("An account with this username already exists.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Department)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Department));

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }

    /// <summary>
    /// Runs the BeUniqueUsernameAsync operation.
    /// </summary>
    private async Task<bool> BeUniqueUsernameAsync(
        CreateUserManagementCommand command,
        string username,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        return existingUser == null;
    }
}

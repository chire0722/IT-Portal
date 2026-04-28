// Project comment: UpdateUserManagementCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Models;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement;

/// <summary>
/// Validates input for the UpdateUserManagementCommand request.
/// </summary>
public sealed class UpdateUserManagementCommandValidator : AbstractValidator<UpdateUserManagementCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserManagementCommandValidator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.Id)
            .NotEmpty();

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
            .MinimumLength(6)
            .When(x => !string.IsNullOrWhiteSpace(x.Password));

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.")
            .When(x => !string.IsNullOrWhiteSpace(x.Password) || !string.IsNullOrWhiteSpace(x.ConfirmPassword));
    }

    /// <summary>
    /// Runs the BeUniqueUsernameAsync operation.
    /// </summary>
    private async Task<bool> BeUniqueUsernameAsync(
        UpdateUserManagementCommand command,
        string username,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        return existingUser == null || existingUser.Id == command.Id;
    }
}

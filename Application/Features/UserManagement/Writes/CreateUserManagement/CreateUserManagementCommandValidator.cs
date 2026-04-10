using FluentValidation;
using IT_Form.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

public sealed class CreateUserManagementCommandValidator : AbstractValidator<CreateUserManagementCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SecuritySettings _securitySettings;

    public CreateUserManagementCommandValidator(
        UserManager<ApplicationUser> userManager,
        IOptions<SecuritySettings> securityOptions)
    {
        _userManager = userManager;
        _securitySettings = securityOptions.Value;

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
            .EmailAddress()
            .Must(HasAllowedEmailDomain)
            .WithMessage(_ => GetAllowedDomainMessage());

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

    private async Task<bool> BeUniqueUsernameAsync(
        CreateUserManagementCommand command,
        string username,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByNameAsync(username);
        return existingUser == null;
    }

    private bool HasAllowedEmailDomain(string? email)
        => UserManagementRules.IsAllowedDomain(email, _securitySettings);

    private string GetAllowedDomainMessage()
        => $"Only emails from the following domains are allowed: {string.Join(", ", _securitySettings.AllowedDomains)}";
}

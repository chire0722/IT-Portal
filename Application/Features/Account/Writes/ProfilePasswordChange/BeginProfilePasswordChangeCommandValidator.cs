// Project comment: BeginProfilePasswordChangeCommandValidator.cs validates profile password-change setup.

using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;

public sealed class BeginProfilePasswordChangeCommandValidator : AbstractValidator<BeginProfilePasswordChangeCommand>
{
    public BeginProfilePasswordChangeCommandValidator(IOptions<IdentityOptions> identityOptions)
    {
        var passwordOptions = identityOptions.Value.Password;

        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");
        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(passwordOptions.RequiredLength)
            .WithMessage($"Password must be at least {passwordOptions.RequiredLength} characters.")
            .Must((command, password) => !string.Equals(command.CurrentPassword, password, StringComparison.Ordinal))
            .WithMessage("New password must be different from the current password.");
        RuleFor(command => command.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Please confirm your new password.")
            .Equal(command => command.NewPassword)
            .WithMessage("Passwords do not match.");

        if (passwordOptions.RequireDigit)
        {
            RuleFor(command => command.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsDigit))
                .WithMessage("Password must contain at least one digit.");
        }

        if (passwordOptions.RequireLowercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsLower))
                .WithMessage("Password must contain at least one lowercase letter.");
        }

        if (passwordOptions.RequireUppercase)
        {
            RuleFor(command => command.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsUpper))
                .WithMessage("Password must contain at least one uppercase letter.");
        }

        if (passwordOptions.RequireNonAlphanumeric)
        {
            RuleFor(command => command.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(ch => !char.IsLetterOrDigit(ch)))
                .WithMessage("Password must contain at least one non-alphanumeric character.");
        }

        if (passwordOptions.RequiredUniqueChars > 0)
        {
            RuleFor(command => command.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Distinct().Count() >= passwordOptions.RequiredUniqueChars)
                .WithMessage($"Password must contain at least {passwordOptions.RequiredUniqueChars} unique characters.");
        }
    }
}

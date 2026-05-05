// Project comment: ForceChangePasswordCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

/// <summary>
/// Validates input for the ForceChangePasswordCommand request.
/// </summary>
public sealed class ForceChangePasswordCommandValidator : AbstractValidator<ForceChangePasswordCommand>
{
    public ForceChangePasswordCommandValidator(IOptions<IdentityOptions> identityOptions)
    {
        var passwordOptions = identityOptions.Value.Password;

        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(passwordOptions.RequiredLength)
            .WithMessage($"Password must be at least {passwordOptions.RequiredLength} characters.")
            .Must((command, password) => string.IsNullOrEmpty(command.CurrentPassword) || !string.Equals(command.CurrentPassword, password, StringComparison.Ordinal))
            .WithMessage("New password must be different from the current password.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");

        if (passwordOptions.RequireDigit)
        {
            RuleFor(x => x.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsDigit))
                .WithMessage("Password must contain at least one digit.");
        }

        if (passwordOptions.RequireLowercase)
        {
            RuleFor(x => x.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsLower))
                .WithMessage("Password must contain at least one lowercase letter.");
        }

        if (passwordOptions.RequireUppercase)
        {
            RuleFor(x => x.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(char.IsUpper))
                .WithMessage("Password must contain at least one uppercase letter.");
        }

        if (passwordOptions.RequireNonAlphanumeric)
        {
            RuleFor(x => x.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(ch => !char.IsLetterOrDigit(ch)))
                .WithMessage("Password must contain at least one non-alphanumeric character.");
        }

        if (passwordOptions.RequiredUniqueChars > 0)
        {
            RuleFor(x => x.NewPassword)
                .Must(password => !string.IsNullOrEmpty(password) && password.Distinct().Count() >= passwordOptions.RequiredUniqueChars)
                .WithMessage($"Password must contain at least {passwordOptions.RequiredUniqueChars} unique characters.");
        }
    }
}

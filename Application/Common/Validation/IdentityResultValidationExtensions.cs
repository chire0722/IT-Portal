using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Common.Validation;

internal static class IdentityResultValidationExtensions
{
    internal static void EnsureSucceeded(this IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationException(result.Errors.Select(ToValidationFailure));
    }

    private static ValidationFailure ToValidationFailure(IdentityError error)
    {
        var propertyName = error.Code switch
        {
            "DuplicateUserName" or "InvalidUserName" => "Username",
            "DuplicateEmail" or "InvalidEmail" => "Email",
            "PasswordMismatch" => "CurrentPassword",
            "PasswordTooShort" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit" or
            "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresUniqueChars" => "Password",
            _ => string.Empty
        };

        return new ValidationFailure(propertyName, error.Description);
    }
}

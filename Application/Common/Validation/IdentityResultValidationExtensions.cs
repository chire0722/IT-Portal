// Project comment: IdentityResultValidationExtensions.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Common.Validation;

/// <summary>
/// Defines the IdentityResultValidationExtensions class used by the IT Form application.
/// </summary>
internal static class IdentityResultValidationExtensions
{
    /// <summary>
    /// Runs the EnsureSucceeded operation.
    /// </summary>
    internal static void EnsureSucceeded(this IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationException(result.Errors.Select(ToValidationFailure));
    }

    /// <summary>
    /// Runs the ToValidationFailure operation.
    /// </summary>
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

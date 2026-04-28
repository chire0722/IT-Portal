// Project comment: LoginAccountCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Account.LoginAccount
{
    /// <summary>
    /// Validates input for the LoginAccountCommand request.
    /// </summary>
    public sealed class LoginAccountCommandValidator : AbstractValidator<LoginAccountCommand>
    {
        public LoginAccountCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.");
        }
    }
}

// Project comment: ForgotPasswordCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Account.ForgotPassword
{
    /// <summary>
    /// Validates input for the ForgotPasswordCommand request.
    /// </summary>
    public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Please enter your username or email address.");
        }
    }
}

// Project comment: ResetPasswordCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Account.ResetPassword
{
    /// <summary>
    /// Validates input for the ResetPasswordCommand request.
    /// </summary>
    public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.");

            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Reset token is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password is required.");
        }
    }
}

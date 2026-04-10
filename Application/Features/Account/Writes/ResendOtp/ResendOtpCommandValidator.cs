using FluentValidation;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.ClientIp)
            .NotEmpty();
    }
}

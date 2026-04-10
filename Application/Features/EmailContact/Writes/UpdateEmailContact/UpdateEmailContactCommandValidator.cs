using FluentValidation;

namespace IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;

public sealed class UpdateEmailContactCommandValidator : AbstractValidator<UpdateEmailContactCommand>
{
    public UpdateEmailContactCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .MaximumLength(150)
            .EmailAddress();

        RuleFor(x => x.Department)
            .MaximumLength(100);
    }
}

// Project comment: CreateEmailContactCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;

/// <summary>
/// Validates input for the CreateEmailContactCommand request.
/// </summary>
public sealed class CreateEmailContactCommandValidator : AbstractValidator<CreateEmailContactCommand>
{
    public CreateEmailContactCommandValidator()
    {
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

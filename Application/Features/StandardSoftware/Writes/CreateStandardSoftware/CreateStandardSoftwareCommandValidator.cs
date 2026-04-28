// Project comment: CreateStandardSoftwareCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;

/// <summary>
/// Validates input for the CreateStandardSoftwareCommand request.
/// </summary>
public sealed class CreateStandardSoftwareCommandValidator : AbstractValidator<CreateStandardSoftwareCommand>
{
    public CreateStandardSoftwareCommandValidator()
    {
        RuleFor(x => x.SoftwareName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

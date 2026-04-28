// Project comment: UpdateStandardSoftwareCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

/// <summary>
/// Validates input for the UpdateStandardSoftwareCommand request.
/// </summary>
public sealed class UpdateStandardSoftwareCommandValidator : AbstractValidator<UpdateStandardSoftwareCommand>
{
    public UpdateStandardSoftwareCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.SoftwareName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

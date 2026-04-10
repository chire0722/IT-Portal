using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;

public sealed class CreateStandardSoftwareCommandValidator : AbstractValidator<CreateStandardSoftwareCommand>
{
    public CreateStandardSoftwareCommandValidator()
    {
        RuleFor(x => x.SoftwareName)
            .NotEmpty()
            .MaximumLength(100);
    }
}

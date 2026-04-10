using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

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

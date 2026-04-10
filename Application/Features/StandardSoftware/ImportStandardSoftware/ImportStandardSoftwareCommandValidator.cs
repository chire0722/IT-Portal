using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    public sealed class ImportStandardSoftwareCommandValidator : AbstractValidator<ImportStandardSoftwareCommand>
    {
        public ImportStandardSoftwareCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please select a valid Excel file.");
        }
    }
}

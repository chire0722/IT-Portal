// Project comment: ImportStandardSoftwareCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    /// <summary>
    /// Validates input for the ImportStandardSoftwareCommand request.
    /// </summary>
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

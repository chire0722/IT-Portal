// Project comment: ImportDepartmentsCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Department.ImportDepartments
{
    /// <summary>
    /// Validates input for the ImportDepartmentsCommand request.
    /// </summary>
    public sealed class ImportDepartmentsCommandValidator : AbstractValidator<ImportDepartmentsCommand>
    {
        public ImportDepartmentsCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .Must(f => f != null && f.Length > 0)
                .WithMessage("Please choose a valid Excel file.");
        }
    }
}

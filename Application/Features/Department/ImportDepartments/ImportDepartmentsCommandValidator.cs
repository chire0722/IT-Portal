using FluentValidation;

namespace IT_Form.Application.Features.Department.ImportDepartments
{
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

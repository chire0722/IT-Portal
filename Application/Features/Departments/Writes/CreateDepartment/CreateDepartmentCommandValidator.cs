using IT_Form.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Departments.Writes.CreateDepartment;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(2, 100)
            .MustAsync(async (name, cancellationToken) =>
                !await context.Departments
                    .AsNoTracking()
                    .AnyAsync(
                        department => department.Name.ToUpper() == name.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Department name must be unique.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

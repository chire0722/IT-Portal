// Project comment: UpdateDepartmentCommandValidator.cs is part of the IT Form application source code.

using IT_Form.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Departments.Writes.UpdateDepartment;

/// <summary>
/// Validates input for the UpdateDepartmentCommand request.
/// </summary>
public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator(ApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(2, 100)
            .MustAsync(async (request, name, cancellationToken) =>
                !await context.Departments
                    .AsNoTracking()
                    .AnyAsync(
                        department => department.Id != request.Id &&
                                      department.Name.ToUpper() == name.Trim().ToUpper(),
                        cancellationToken))
            .WithMessage("Department name must be unique.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

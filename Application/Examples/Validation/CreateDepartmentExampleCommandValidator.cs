// Project comment: CreateDepartmentExampleCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Examples.Validation;

/// <summary>
/// Validates input for the CreateDepartmentExampleCommand request.
/// </summary>
public sealed class CreateDepartmentExampleCommandValidator : AbstractValidator<CreateDepartmentExampleCommand>
{
    public CreateDepartmentExampleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 50);
    }
}

using FluentValidation;

namespace IT_Form.Application.Examples.Validation;

public sealed class CreateDepartmentExampleCommandValidator : AbstractValidator<CreateDepartmentExampleCommand>
{
    public CreateDepartmentExampleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(2, 50);
    }
}

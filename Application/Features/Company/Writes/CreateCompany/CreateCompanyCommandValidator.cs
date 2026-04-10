using FluentValidation;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Lot)
            .MaximumLength(100);

        RuleFor(x => x.Logo)
            .NotNull()
            .Must(file => file != null && file.Length > 0)
            .WithMessage("Logo is required.");
    }
}

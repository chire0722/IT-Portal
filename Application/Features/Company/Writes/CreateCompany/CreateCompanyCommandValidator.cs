using FluentValidation;
using IT_Form.Helpers;

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

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .Must(CompanyTimeZoneHelper.IsSupportedTimeZoneId)
            .WithMessage("Please select a valid time zone.");

        RuleFor(x => x.BusinessEndTime)
            .GreaterThan(x => x.BusinessStartTime)
            .WithMessage("Business end time must be later than the start time.");

        RuleFor(x => x)
            .Must(HasAnyBusinessDay)
            .WithMessage("Select at least one business day.");

        RuleFor(x => x.Logo)
            .NotNull()
            .Must(file => file != null && file.Length > 0)
            .WithMessage("Logo is required.");
    }

    private static bool HasAnyBusinessDay(CreateCompanyCommand command)
        => command.WorksOnMonday ||
           command.WorksOnTuesday ||
           command.WorksOnWednesday ||
           command.WorksOnThursday ||
           command.WorksOnFriday ||
           command.WorksOnSaturday ||
           command.WorksOnSunday;
}

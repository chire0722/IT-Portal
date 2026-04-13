using FluentValidation;
using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Company.Writes.UpdateCompany;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    private readonly ApplicationDbContext _context;

    public UpdateCompanyCommandValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Id)
            .GreaterThan(0);

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

        RuleFor(x => x.IsLoginPageLogo)
            .CustomAsync(ValidateLoginPageLogoAsync);
    }

    private async Task ValidateLoginPageLogoAsync(
        bool isLoginPageLogo,
        ValidationContext<UpdateCompanyCommand> context,
        CancellationToken cancellationToken)
    {
        if (!isLoginPageLogo)
        {
            return;
        }

        var command = context.InstanceToValidate;
        if (command.Logo != null && command.Logo.Length > 0)
        {
            return;
        }

        var existingCompany = await _context.Companies
            .AsNoTracking()
            .Where(company => company.Id == command.Id)
            .Select(company => new { company.LogoPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCompany == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(existingCompany.LogoPath))
        {
            context.AddFailure(
                nameof(UpdateCompanyCommand.IsLoginPageLogo),
                "A logo is required before using it on the login page.");
        }
    }

    private static bool HasAnyBusinessDay(UpdateCompanyCommand command)
        => command.WorksOnMonday ||
           command.WorksOnTuesday ||
           command.WorksOnWednesday ||
           command.WorksOnThursday ||
           command.WorksOnFriday ||
           command.WorksOnSaturday ||
           command.WorksOnSunday;
}

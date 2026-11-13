// Project comment: CreateCompanyCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Http;

namespace IT_Form.Application.Features.Company.Writes.CreateCompany;

/// <summary>
/// Validates input for the CreateCompanyCommand request.
/// </summary>
public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".gif"];
    private static readonly string[] AllowedLogoContentTypes = ["image/png", "image/jpeg", "image/gif"];
    private const long MaxLogoBytes = 1 * 1024 * 1024;

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

        RuleFor(x => x.Logo)
            .Must(file => file == null || file.Length <= MaxLogoBytes)
            .WithMessage("Logo must be under 1 MB.");

        RuleFor(x => x.Logo)
            .Must(BeAllowedLogoFile)
            .WithMessage("Only PNG, JPG, and GIF logo images are allowed.");
    }

    /// <summary>
    /// Runs the HasAnyBusinessDay operation.
    /// </summary>
    private static bool HasAnyBusinessDay(CreateCompanyCommand command)
        => command.WorksOnMonday ||
           command.WorksOnTuesday ||
           command.WorksOnWednesday ||
           command.WorksOnThursday ||
           command.WorksOnFriday ||
           command.WorksOnSaturday ||
           command.WorksOnSunday;

    /// <summary>
    /// Runs the BeAllowedLogoFile operation.
    /// </summary>
    private static bool BeAllowedLogoFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return true;
        }

        var extension = (Path.GetExtension(file.FileName ?? string.Empty) ?? string.Empty).ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        return AllowedLogoExtensions.Contains(extension) &&
               AllowedLogoContentTypes.Contains(contentType) &&
               UploadFileSignatureValidator.HasAllowedSignature(file, AllowedLogoExtensions);
    }
}


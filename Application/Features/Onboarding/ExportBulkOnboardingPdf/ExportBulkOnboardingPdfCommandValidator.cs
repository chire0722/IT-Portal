// Project comment: ExportBulkOnboardingPdfCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
    /// <summary>
    /// Validates input for the ExportBulkOnboardingPdfCommand request.
    /// </summary>
    public sealed class ExportBulkOnboardingPdfCommandValidator : AbstractValidator<ExportBulkOnboardingPdfCommand>
    {
        public ExportBulkOnboardingPdfCommandValidator()
        {
            RuleFor(x => x.Ids)
                .NotNull()
                .Must(ids => ids.Length > 0)
                .WithMessage("At least one onboarding request must be selected.");
        }
    }
}

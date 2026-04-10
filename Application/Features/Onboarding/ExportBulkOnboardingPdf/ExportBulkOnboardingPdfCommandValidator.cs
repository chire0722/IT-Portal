using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
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

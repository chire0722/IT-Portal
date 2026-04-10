using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
    public sealed class ExportBulkOffboardingPdfCommandValidator : AbstractValidator<ExportBulkOffboardingPdfCommand>
    {
        public ExportBulkOffboardingPdfCommandValidator()
        {
            RuleFor(x => x.Ids)
                .NotNull()
                .Must(ids => ids.Length > 0)
                .WithMessage("At least one offboarding request must be selected.");
        }
    }
}

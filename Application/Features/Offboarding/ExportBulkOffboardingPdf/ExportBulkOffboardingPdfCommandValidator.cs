// Project comment: ExportBulkOffboardingPdfCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
    /// <summary>
    /// Validates input for the ExportBulkOffboardingPdfCommand request.
    /// </summary>
    public sealed class ExportBulkOffboardingPdfCommandValidator : AbstractValidator<ExportBulkOffboardingPdfCommand>
    {
        private const int MaxSelectedRequests = 100;

        public ExportBulkOffboardingPdfCommandValidator()
        {
            RuleFor(x => x.Ids)
                .NotNull()
                .Must(ids => ids.Length > 0)
                .WithMessage("At least one offboarding request must be selected.");
            RuleFor(x => x.Ids)
                .Must(ids => ids.Length <= MaxSelectedRequests)
                .WithMessage($"Select no more than {MaxSelectedRequests} requests per export.");
        }
    }
}


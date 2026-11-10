// Project comment: ExportBulkOnboardingPdfCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
    /// <summary>
    /// Validates input for the ExportBulkOnboardingPdfCommand request.
    /// </summary>
    public sealed class ExportBulkOnboardingPdfCommandValidator : AbstractValidator<ExportBulkOnboardingPdfCommand>
    {
        private const int MaxSelectedRequests = 100;

        public ExportBulkOnboardingPdfCommandValidator()
        {
            RuleFor(x => x.Ids)
                .NotNull()
                .Must(ids => ids.Length > 0)
                .WithMessage("At least one onboarding request must be selected.");
            RuleFor(x => x.Ids)
                .Must(ids => ids.Length <= MaxSelectedRequests)
                .WithMessage($"Select no more than {MaxSelectedRequests} requests per export.");
        }
    }
}


// Project comment: ExportBulkOnboardingPdfCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
    /// <summary>
    /// Carries data needed to execute the ExportBulkOnboardingPdf command.
    /// </summary>
    public sealed record ExportBulkOnboardingPdfCommand(int[] Ids) : IRequest<ProcessResult<BinaryFilePayload>>;
}

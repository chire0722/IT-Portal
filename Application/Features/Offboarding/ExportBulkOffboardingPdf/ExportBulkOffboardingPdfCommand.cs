// Project comment: ExportBulkOffboardingPdfCommand.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
    /// <summary>
    /// Carries data needed to execute the ExportBulkOffboardingPdf command.
    /// </summary>
    public sealed record ExportBulkOffboardingPdfCommand(int[] Ids) : IRequest<ProcessResult<BinaryFilePayload>>;
}

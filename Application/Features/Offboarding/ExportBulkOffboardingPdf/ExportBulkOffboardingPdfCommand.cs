using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ExportBulkOffboardingPdf
{
    public sealed record ExportBulkOffboardingPdfCommand(int[] Ids) : IRequest<ProcessResult<BinaryFilePayload>>;
}

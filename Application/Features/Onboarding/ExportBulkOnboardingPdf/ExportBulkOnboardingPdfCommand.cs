using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ExportBulkOnboardingPdf
{
    public sealed record ExportBulkOnboardingPdfCommand(int[] Ids) : IRequest<ProcessResult<BinaryFilePayload>>;
}

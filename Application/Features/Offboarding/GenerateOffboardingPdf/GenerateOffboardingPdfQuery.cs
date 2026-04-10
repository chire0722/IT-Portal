using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.GenerateOffboardingPdf
{
    public sealed record GenerateOffboardingPdfQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}

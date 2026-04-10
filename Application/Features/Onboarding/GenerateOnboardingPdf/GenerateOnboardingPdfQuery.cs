using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GenerateOnboardingPdf
{
    public sealed record GenerateOnboardingPdfQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}

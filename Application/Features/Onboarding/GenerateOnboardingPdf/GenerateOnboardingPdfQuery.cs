// Project comment: GenerateOnboardingPdfQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GenerateOnboardingPdf
{
    /// <summary>
    /// Carries data needed to execute the GenerateOnboardingPdf query.
    /// </summary>
    public sealed record GenerateOnboardingPdfQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}

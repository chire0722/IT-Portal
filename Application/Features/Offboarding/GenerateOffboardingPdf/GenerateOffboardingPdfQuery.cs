// Project comment: GenerateOffboardingPdfQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.GenerateOffboardingPdf
{
    /// <summary>
    /// Carries data needed to execute the GenerateOffboardingPdf query.
    /// </summary>
    public sealed record GenerateOffboardingPdfQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}

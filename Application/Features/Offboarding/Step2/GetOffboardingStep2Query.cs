// Project comment: GetOffboardingStep2Query.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    /// <summary>
    /// Carries data needed to execute the GetOffboardingStep2 query.
    /// </summary>
    public sealed record GetOffboardingStep2Query(int Id) : IRequest<ProcessResult<OffboardingRequest>>;
}

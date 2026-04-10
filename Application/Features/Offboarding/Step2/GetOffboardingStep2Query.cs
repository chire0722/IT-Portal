using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    public sealed record GetOffboardingStep2Query(int Id) : IRequest<ProcessResult<OffboardingRequest>>;
}

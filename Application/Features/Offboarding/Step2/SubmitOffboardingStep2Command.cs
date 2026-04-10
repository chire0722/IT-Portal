using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step2
{
    public sealed record SubmitOffboardingStep2Command(int Id, string SaveAction, OffboardingRequest Request) : IRequest<ProcessResult<object>>;
}

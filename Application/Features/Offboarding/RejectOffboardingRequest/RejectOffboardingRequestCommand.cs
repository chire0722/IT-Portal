using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    public sealed record RejectOffboardingRequestCommand(int Id, string Reason) : IRequest<ProcessResult<object>>;
}

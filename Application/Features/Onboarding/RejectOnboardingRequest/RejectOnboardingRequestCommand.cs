using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    public sealed record RejectOnboardingRequestCommand(int Id, string Reason) : IRequest<ProcessResult<object>>;
}

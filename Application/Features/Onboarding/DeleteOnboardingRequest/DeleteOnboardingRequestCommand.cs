using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    public sealed record DeleteOnboardingRequestCommand(int Id, bool IsAjax) : IRequest<ProcessResult<object>>;
}

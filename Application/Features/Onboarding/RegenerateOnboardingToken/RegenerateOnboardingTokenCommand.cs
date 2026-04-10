using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    public sealed record RegenerateOnboardingTokenCommand(int Id) : IRequest<ProcessResult<object>>;
}

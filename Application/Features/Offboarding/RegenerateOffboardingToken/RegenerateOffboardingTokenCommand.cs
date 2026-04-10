using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    public sealed record RegenerateOffboardingTokenCommand(int Id) : IRequest<ProcessResult<object>>;
}

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    public sealed record ProcessOffboardingTokenRegenerationCommand(int Id) : IRequest<CommandResponse>;
}

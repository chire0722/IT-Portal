using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    public sealed record ProcessOffboardingRejectionCommand(int Id, string Reason) : IRequest<CommandResponse>;
}

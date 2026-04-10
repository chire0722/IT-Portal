using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.DeleteOffboardingRequest
{
    public sealed record DeleteOffboardingRequestCommand(int Id) : IRequest<CommandResponse>;
}

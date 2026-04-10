using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed record ProcessOnboardingTokenRegenerationCommand(int Id) : IRequest<CommandResponse>;
}

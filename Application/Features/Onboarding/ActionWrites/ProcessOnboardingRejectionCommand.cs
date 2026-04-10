using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ActionWrites
{
    public sealed record ProcessOnboardingRejectionCommand(int Id, string Reason) : IRequest<CommandResponse>;
}

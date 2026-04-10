using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    public sealed record EditOnboardingRequestCommand(
        int Id,
        OnboardingRequest Model,
        IReadOnlyCollection<int> SelectedStandardSoftwareIds,
        IReadOnlyCollection<int> SelectedEmailGroupIds,
        IReadOnlyCollection<int> SelectedSharedMailboxIds,
        string ActionType) : IRequest<EditOnboardingRequestResult>;
}

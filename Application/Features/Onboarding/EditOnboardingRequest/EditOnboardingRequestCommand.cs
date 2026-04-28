// Project comment: EditOnboardingRequestCommand.cs is part of the IT Form application source code.

using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    /// <summary>
    /// Carries data needed to execute the EditOnboardingRequest command.
    /// </summary>
    public sealed record EditOnboardingRequestCommand(
        int Id,
        OnboardingRequest Model,
        IReadOnlyCollection<int> SelectedStandardSoftwareIds,
        IReadOnlyCollection<int> SelectedEmailGroupIds,
        IReadOnlyCollection<int> SelectedSharedMailboxIds,
        string ActionType) : IRequest<EditOnboardingRequestResult>;
}

// Project comment: UpdateOnboardingItStep4Command.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    /// <summary>
    /// Carries data needed to execute the UpdateOnboardingItStep4 command.
    /// </summary>
    public sealed record UpdateOnboardingItStep4Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        bool IsDraft) : IRequest<UpdateOnboardingItStep4Result>;
}

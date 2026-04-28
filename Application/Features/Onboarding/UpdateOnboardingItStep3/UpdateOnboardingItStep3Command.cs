// Project comment: UpdateOnboardingItStep3Command.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    /// <summary>
    /// Carries data needed to execute the UpdateOnboardingItStep3 command.
    /// </summary>
    public sealed record UpdateOnboardingItStep3Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        bool IsDraft) : IRequest<UpdateOnboardingItStep3Result>;
}

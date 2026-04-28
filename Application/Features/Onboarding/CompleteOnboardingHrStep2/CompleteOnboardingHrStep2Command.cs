// Project comment: CompleteOnboardingHrStep2Command.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    /// <summary>
    /// Carries data needed to execute the CompleteOnboardingHrStep2 command.
    /// </summary>
    public sealed record CompleteOnboardingHrStep2Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        string? CreatedByUserName) : IRequest<CompleteOnboardingHrStep2Result>;
}

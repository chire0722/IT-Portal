// Project comment: SaveOnboardingStep1Command.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    /// <summary>
    /// Carries data needed to execute the SaveOnboardingStep1 command.
    /// </summary>
    public sealed record SaveOnboardingStep1Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session) : IRequest<SaveOnboardingStep1Result>;
}

// Project comment: GetOnboardingStep1OptionsQuery.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingStep1Options
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingStep1Options query.
    /// </summary>
    public sealed record GetOnboardingStep1OptionsQuery(
        OnboardingViewModel Model) : IRequest<OnboardingStep1PageViewModel>;
}

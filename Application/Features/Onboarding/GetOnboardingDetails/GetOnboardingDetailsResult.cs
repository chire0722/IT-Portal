// Project comment: GetOnboardingDetailsResult.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    /// <summary>
    /// Lists the supported GetOnboardingDetailsStatus values used by the application.
    /// </summary>
    public enum GetOnboardingDetailsStatus
    {
        Ready,
        NotFound,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingDetails workflow.
    /// </summary>
    public sealed record GetOnboardingDetailsResult(
        GetOnboardingDetailsStatus Status,
        OnboardingDetailsPageViewModel? ViewModel);
}

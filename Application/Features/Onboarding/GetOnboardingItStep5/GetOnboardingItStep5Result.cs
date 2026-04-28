// Project comment: GetOnboardingItStep5Result.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep5
{
    /// <summary>
    /// Lists the supported GetOnboardingItStep5Status values used by the application.
    /// </summary>
    public enum GetOnboardingItStep5Status
    {
        Ready,
        SessionExpired
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingItStep5 workflow.
    /// </summary>
    public sealed record GetOnboardingItStep5Result(
        GetOnboardingItStep5Status Status,
        OnboardingViewModel? SessionModel,
        OnboardingStep5PageViewModel? ViewModel,
        string? Message,
        int RequestId);
}

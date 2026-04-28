// Project comment: GetOnboardingItSetupResult.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItSetup
{
    /// <summary>
    /// Lists the supported GetOnboardingItSetupStatus values used by the application.
    /// </summary>
    public enum GetOnboardingItSetupStatus
    {
        Ready,
        NotFound,
        Locked
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingItSetup workflow.
    /// </summary>
    public sealed record GetOnboardingItSetupResult(
        GetOnboardingItSetupStatus Status,
        OnboardingViewModel? Session,
        int RequestId,
        string? Message);
}

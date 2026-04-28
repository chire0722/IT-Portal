// Project comment: SignOffHrOnboardingResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    /// <summary>
    /// Lists the supported SignOffHrOnboardingStatus values used by the application.
    /// </summary>
    public enum SignOffHrOnboardingStatus
    {
        SignedOff,
        NotFound,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the SignOffHrOnboarding workflow.
    /// </summary>
    public sealed record SignOffHrOnboardingResult(
        SignOffHrOnboardingStatus Status,
        int RequestId,
        string Message);
}


// Project comment: CompleteOnboardingHrStep2Result.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    /// <summary>
    /// Lists the supported CompleteOnboardingHrStep2Status values used by the application.
    /// </summary>
    public enum CompleteOnboardingHrStep2Status
    {
        Completed,
        MissingStep1,
        NotFound,
        Locked,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the CompleteOnboardingHrStep2 workflow.
    /// </summary>
    public sealed record CompleteOnboardingHrStep2Result(
        CompleteOnboardingHrStep2Status Status,
        int RequestId,
        string Message);
}

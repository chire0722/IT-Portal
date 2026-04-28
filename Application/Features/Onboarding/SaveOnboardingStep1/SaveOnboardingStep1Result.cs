// Project comment: SaveOnboardingStep1Result.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    /// <summary>
    /// Lists the supported SaveOnboardingStep1Status values used by the application.
    /// </summary>
    public enum SaveOnboardingStep1Status
    {
        ReadyForStep2,
        Locked,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the SaveOnboardingStep1 workflow.
    /// </summary>
    public sealed record SaveOnboardingStep1Result(
        SaveOnboardingStep1Status Status,
        int RequestId,
        string Message);
}

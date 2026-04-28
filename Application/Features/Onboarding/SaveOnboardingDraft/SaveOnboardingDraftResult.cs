// Project comment: SaveOnboardingDraftResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    /// <summary>
    /// Lists the supported SaveOnboardingDraftStatus values used by the application.
    /// </summary>
    public enum SaveOnboardingDraftStatus
    {
        Saved,
        NotFound,
        InvalidState,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the SaveOnboardingDraft workflow.
    /// </summary>
    public sealed record SaveOnboardingDraftResult(
        SaveOnboardingDraftStatus Status,
        int RequestId,
        string Message);
}

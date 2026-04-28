// Project comment: UpdateOnboardingChecklistResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    /// <summary>
    /// Lists the supported UpdateOnboardingChecklistStatus values used by the application.
    /// </summary>
    public enum UpdateOnboardingChecklistStatus
    {
        Updated,
        NotFound,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the UpdateOnboardingChecklist workflow.
    /// </summary>
    public sealed record UpdateOnboardingChecklistResult(
        UpdateOnboardingChecklistStatus Status,
        int RequestId,
        string Message);
}

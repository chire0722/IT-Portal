// Project comment: UpdateOnboardingItStep3Result.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    /// <summary>
    /// Lists the supported UpdateOnboardingItStep3Status values used by the application.
    /// </summary>
    public enum UpdateOnboardingItStep3Status
    {
        ReadyForStep4,
        DraftSaved,
        NotFound,
        Forbidden,
        Locked
    }

    /// <summary>
    /// Represents the result returned by the UpdateOnboardingItStep3 workflow.
    /// </summary>
    public sealed record UpdateOnboardingItStep3Result(
        UpdateOnboardingItStep3Status Status,
        int RequestId,
        string Message);
}

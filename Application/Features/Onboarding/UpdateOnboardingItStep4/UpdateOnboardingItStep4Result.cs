// Project comment: UpdateOnboardingItStep4Result.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    /// <summary>
    /// Lists the supported UpdateOnboardingItStep4Status values used by the application.
    /// </summary>
    public enum UpdateOnboardingItStep4Status
    {
        ReadyForStep5,
        DraftSaved,
        NotFound,
        Forbidden,
        Locked
    }

    /// <summary>
    /// Represents the result returned by the UpdateOnboardingItStep4 workflow.
    /// </summary>
    public sealed record UpdateOnboardingItStep4Result(
        UpdateOnboardingItStep4Status Status,
        int RequestId,
        string Message);
}

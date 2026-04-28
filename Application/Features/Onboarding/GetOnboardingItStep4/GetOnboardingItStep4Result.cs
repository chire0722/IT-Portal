// Project comment: GetOnboardingItStep4Result.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep4
{
    /// <summary>
    /// Lists the supported GetOnboardingItStep4Status values used by the application.
    /// </summary>
    public enum GetOnboardingItStep4Status
    {
        Ready,
        SessionExpired,
        NotFound,
        Forbidden,
        Locked
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingItStep4 workflow.
    /// </summary>
    public sealed record GetOnboardingItStep4Result(
        GetOnboardingItStep4Status Status,
        OnboardingViewModel? Model,
        string? Message,
        int RequestId);
}

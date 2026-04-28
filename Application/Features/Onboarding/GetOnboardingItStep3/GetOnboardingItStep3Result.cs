// Project comment: GetOnboardingItStep3Result.cs is part of the IT Form application source code.

using IT_Form.Models;
using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep3
{
    /// <summary>
    /// Lists the supported GetOnboardingItStep3Status values used by the application.
    /// </summary>
    public enum GetOnboardingItStep3Status
    {
        Ready,
        SessionExpired,
        NotFound,
        Forbidden,
        Locked
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingItStep3 workflow.
    /// </summary>
    public sealed record GetOnboardingItStep3Result(
        GetOnboardingItStep3Status Status,
        OnboardingViewModel? SessionModel,
        OnboardingStep3PageViewModel? ViewModel,
        string? Message,
        int RequestId);
}

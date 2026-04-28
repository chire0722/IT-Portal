// Project comment: GetOnboardingEditResult.cs is part of the IT Form application source code.

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    /// <summary>
    /// Lists the supported GetOnboardingEditStatus values used by the application.
    /// </summary>
    public enum GetOnboardingEditStatus
    {
        Ready,
        NotFound,
        Forbidden,
        Locked,
        Draft
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingEdit workflow.
    /// </summary>
    public sealed record GetOnboardingEditResult(
        GetOnboardingEditStatus Status,
        EditOnboardingPageViewModel? ViewModel,
        string? Message);
}

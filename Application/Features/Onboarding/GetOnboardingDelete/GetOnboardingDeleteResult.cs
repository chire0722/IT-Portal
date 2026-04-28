// Project comment: GetOnboardingDeleteResult.cs is part of the IT Form application source code.

using IT_Form.Models;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDelete
{
    /// <summary>
    /// Lists the supported GetOnboardingDeleteStatus values used by the application.
    /// </summary>
    public enum GetOnboardingDeleteStatus
    {
        Ready,
        NotFound,
        Forbidden,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the GetOnboardingDelete workflow.
    /// </summary>
    public sealed record GetOnboardingDeleteResult(
        GetOnboardingDeleteStatus Status,
        OnboardingRequest? Request,
        string? Message);
}

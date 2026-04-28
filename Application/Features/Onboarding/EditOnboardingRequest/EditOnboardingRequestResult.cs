// Project comment: EditOnboardingRequestResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    /// <summary>
    /// Lists the supported EditOnboardingRequestStatus values used by the application.
    /// </summary>
    public enum EditOnboardingRequestStatus
    {
        Updated,
        Submitted,
        NotFound,
        Locked,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the EditOnboardingRequest workflow.
    /// </summary>
    public sealed record EditOnboardingRequestResult(
        EditOnboardingRequestStatus Status,
        int RequestId,
        string Message,
        bool ShouldAutoOpenEmail = false);
}

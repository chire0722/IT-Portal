// Project comment: RejectOnboardingRequestResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    /// <summary>
    /// Lists the supported RejectOnboardingRequestStatus values used by the application.
    /// </summary>
    public enum RejectOnboardingRequestStatus
    {
        Rejected,
        NotFound,
        ForbiddenRole,
        ForbiddenScope,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the RejectOnboardingRequest workflow.
    /// </summary>
    public sealed record RejectOnboardingRequestResult(
        RejectOnboardingRequestStatus Status,
        int RequestId,
        string Message);
}

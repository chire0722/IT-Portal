// Project comment: RejectOffboardingRequestResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    /// <summary>
    /// Lists the supported RejectOffboardingRequestStatus values used by the application.
    /// </summary>
    public enum RejectOffboardingRequestStatus
    {
        Rejected,
        NotFound,
        ForbiddenRole,
        ForbiddenScope,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the RejectOffboardingRequest workflow.
    /// </summary>
    public sealed record RejectOffboardingRequestResult(
        RejectOffboardingRequestStatus Status,
        int RequestId,
        string Message);
}

// Project comment: DeleteOnboardingRequestResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    /// <summary>
    /// Lists the supported DeleteOnboardingRequestStatus values used by the application.
    /// </summary>
    public enum DeleteOnboardingRequestStatus
    {
        Deleted,
        NotFound,
        ForbiddenScope,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the DeleteOnboardingRequest workflow.
    /// </summary>
    public sealed record DeleteOnboardingRequestResult(
        DeleteOnboardingRequestStatus Status,
        int RequestId,
        string Message,
        string? EmployeeName);
}


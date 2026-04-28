// Project comment: SignOnboardingRequestResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    /// <summary>
    /// Lists the supported SignOnboardingRequestStatus values used by the application.
    /// </summary>
    public enum SignOnboardingRequestStatus
    {
        Signed,
        NotFound,
        ForbiddenScope,
        ForbiddenRole,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the SignOnboardingRequest workflow.
    /// </summary>
    public sealed record SignOnboardingRequestResult(
        SignOnboardingRequestStatus Status,
        int RequestId,
        string Message);
}

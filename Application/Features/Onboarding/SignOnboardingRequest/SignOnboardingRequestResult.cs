namespace IT_Form.Application.Features.Onboarding.SignOnboardingRequest
{
    public enum SignOnboardingRequestStatus
    {
        Signed,
        NotFound,
        ForbiddenScope,
        ForbiddenRole,
        InvalidState
    }

    public sealed record SignOnboardingRequestResult(
        SignOnboardingRequestStatus Status,
        int RequestId,
        string Message);
}

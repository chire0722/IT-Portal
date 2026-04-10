namespace IT_Form.Application.Features.Onboarding.RejectOnboardingRequest
{
    public enum RejectOnboardingRequestStatus
    {
        Rejected,
        NotFound,
        ForbiddenRole,
        ForbiddenScope,
        InvalidState
    }

    public sealed record RejectOnboardingRequestResult(
        RejectOnboardingRequestStatus Status,
        int RequestId,
        string Message);
}

namespace IT_Form.Application.Features.Offboarding.RejectOffboardingRequest
{
    public enum RejectOffboardingRequestStatus
    {
        Rejected,
        NotFound,
        ForbiddenRole,
        ForbiddenScope,
        InvalidState
    }

    public sealed record RejectOffboardingRequestResult(
        RejectOffboardingRequestStatus Status,
        int RequestId,
        string Message);
}

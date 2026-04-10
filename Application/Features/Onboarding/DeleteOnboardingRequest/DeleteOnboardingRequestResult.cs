namespace IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest
{
    public enum DeleteOnboardingRequestStatus
    {
        Deleted,
        NotFound,
        ForbiddenScope,
        InvalidState
    }

    public sealed record DeleteOnboardingRequestResult(
        DeleteOnboardingRequestStatus Status,
        int RequestId,
        string Message,
        string? EmployeeName);
}


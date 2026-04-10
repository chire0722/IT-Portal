namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    public enum EditOnboardingRequestStatus
    {
        Updated,
        Submitted,
        NotFound,
        Locked,
        Forbidden
    }

    public sealed record EditOnboardingRequestResult(
        EditOnboardingRequestStatus Status,
        int RequestId,
        string Message,
        bool ShouldAutoOpenEmail = false);
}

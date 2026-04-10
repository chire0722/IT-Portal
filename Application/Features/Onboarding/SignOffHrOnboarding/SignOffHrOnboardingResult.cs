namespace IT_Form.Application.Features.Onboarding.SignOffHrOnboarding
{
    public enum SignOffHrOnboardingStatus
    {
        SignedOff,
        NotFound,
        InvalidState
    }

    public sealed record SignOffHrOnboardingResult(
        SignOffHrOnboardingStatus Status,
        int RequestId,
        string Message);
}


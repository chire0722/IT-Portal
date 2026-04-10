namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    public enum CompleteOnboardingHrStep2Status
    {
        Completed,
        MissingStep1,
        NotFound,
        Locked,
        Forbidden
    }

    public sealed record CompleteOnboardingHrStep2Result(
        CompleteOnboardingHrStep2Status Status,
        int RequestId,
        string Message);
}

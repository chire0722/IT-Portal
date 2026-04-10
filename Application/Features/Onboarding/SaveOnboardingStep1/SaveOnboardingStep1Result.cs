namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    public enum SaveOnboardingStep1Status
    {
        ReadyForStep2,
        Locked,
        Forbidden
    }

    public sealed record SaveOnboardingStep1Result(
        SaveOnboardingStep1Status Status,
        int RequestId,
        string Message);
}

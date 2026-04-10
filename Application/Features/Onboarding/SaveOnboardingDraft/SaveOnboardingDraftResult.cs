namespace IT_Form.Application.Features.Onboarding.SaveOnboardingDraft
{
    public enum SaveOnboardingDraftStatus
    {
        Saved,
        NotFound,
        InvalidState,
        Forbidden
    }

    public sealed record SaveOnboardingDraftResult(
        SaveOnboardingDraftStatus Status,
        int RequestId,
        string Message);
}

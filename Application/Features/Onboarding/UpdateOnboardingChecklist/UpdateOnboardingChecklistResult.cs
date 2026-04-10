namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingChecklist
{
    public enum UpdateOnboardingChecklistStatus
    {
        Updated,
        NotFound,
        InvalidState
    }

    public sealed record UpdateOnboardingChecklistResult(
        UpdateOnboardingChecklistStatus Status,
        int RequestId,
        string Message);
}

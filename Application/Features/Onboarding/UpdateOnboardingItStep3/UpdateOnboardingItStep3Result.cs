namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    public enum UpdateOnboardingItStep3Status
    {
        ReadyForStep4,
        DraftSaved,
        NotFound,
        Forbidden,
        Locked
    }

    public sealed record UpdateOnboardingItStep3Result(
        UpdateOnboardingItStep3Status Status,
        int RequestId,
        string Message);
}

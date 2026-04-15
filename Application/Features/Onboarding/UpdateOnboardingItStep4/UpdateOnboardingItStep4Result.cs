namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    public enum UpdateOnboardingItStep4Status
    {
        ReadyForStep5,
        DraftSaved,
        NotFound,
        Forbidden,
        Locked
    }

    public sealed record UpdateOnboardingItStep4Result(
        UpdateOnboardingItStep4Status Status,
        int RequestId,
        string Message);
}

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep4
{
    public enum GetOnboardingItStep4Status
    {
        Ready,
        SessionExpired,
        NotFound,
        Locked
    }

    public sealed record GetOnboardingItStep4Result(
        GetOnboardingItStep4Status Status,
        OnboardingViewModel? Model,
        string? Message,
        int RequestId);
}

using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItSetup
{
    public enum GetOnboardingItSetupStatus
    {
        Ready,
        NotFound,
        Locked
    }

    public sealed record GetOnboardingItSetupResult(
        GetOnboardingItSetupStatus Status,
        OnboardingViewModel? Session,
        int RequestId,
        string? Message);
}

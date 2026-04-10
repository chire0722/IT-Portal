using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    public enum GetOnboardingDetailsStatus
    {
        Ready,
        NotFound,
        Forbidden
    }

    public sealed record GetOnboardingDetailsResult(
        GetOnboardingDetailsStatus Status,
        OnboardingDetailsPageViewModel? ViewModel);
}

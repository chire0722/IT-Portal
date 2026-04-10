using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep5
{
    public enum GetOnboardingItStep5Status
    {
        Ready,
        SessionExpired
    }

    public sealed record GetOnboardingItStep5Result(
        GetOnboardingItStep5Status Status,
        OnboardingViewModel? SessionModel,
        OnboardingStep5PageViewModel? ViewModel,
        string? Message,
        int RequestId);
}

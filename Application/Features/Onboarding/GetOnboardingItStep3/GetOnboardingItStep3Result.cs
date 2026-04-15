using IT_Form.Models;
using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep3
{
    public enum GetOnboardingItStep3Status
    {
        Ready,
        SessionExpired,
        NotFound,
        Forbidden,
        Locked
    }

    public sealed record GetOnboardingItStep3Result(
        GetOnboardingItStep3Status Status,
        OnboardingViewModel? SessionModel,
        OnboardingStep3PageViewModel? ViewModel,
        string? Message,
        int RequestId);
}

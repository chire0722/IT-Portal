using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    public enum GetOnboardingEditStatus
    {
        Ready,
        NotFound,
        Forbidden,
        Locked,
        Draft
    }

    public sealed record GetOnboardingEditResult(
        GetOnboardingEditStatus Status,
        EditOnboardingPageViewModel? ViewModel,
        string? Message);
}

using IT_Form.Models;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDelete
{
    public enum GetOnboardingDeleteStatus
    {
        Ready,
        NotFound,
        Forbidden,
        InvalidState
    }

    public sealed record GetOnboardingDeleteResult(
        GetOnboardingDeleteStatus Status,
        OnboardingRequest? Request,
        string? Message);
}

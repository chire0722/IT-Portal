using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingHrStep2
{
    public sealed record CompleteOnboardingHrStep2Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        string? CreatedByUserName) : IRequest<CompleteOnboardingHrStep2Result>;
}

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.SaveOnboardingStep1
{
    public sealed record SaveOnboardingStep1Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session) : IRequest<SaveOnboardingStep1Result>;
}

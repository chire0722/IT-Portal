using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingStep1Options
{
    public sealed record GetOnboardingStep1OptionsQuery(
        OnboardingViewModel Model) : IRequest<OnboardingStep1PageViewModel>;
}

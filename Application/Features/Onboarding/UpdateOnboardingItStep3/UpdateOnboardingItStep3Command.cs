using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep3
{
    public sealed record UpdateOnboardingItStep3Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        bool IsDraft) : IRequest<UpdateOnboardingItStep3Result>;
}

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateOnboardingItStep4
{
    public sealed record UpdateOnboardingItStep4Command(
        OnboardingViewModel Model,
        OnboardingViewModel Session,
        bool IsDraft) : IRequest<UpdateOnboardingItStep4Result>;
}

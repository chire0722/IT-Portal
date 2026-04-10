using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep4
{
    public sealed record GetOnboardingItStep4Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep4Result>;
}

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep3
{
    public sealed record GetOnboardingItStep3Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep3Result>;
}

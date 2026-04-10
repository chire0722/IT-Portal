using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep5
{
    public sealed record GetOnboardingItStep5Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep5Result>;
}

using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEditOptions
{
    public sealed record GetOnboardingEditOptionsQuery(
        OnboardingRequest Model) : IRequest<EditOnboardingPageViewModel>;
}

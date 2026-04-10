using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItSetup
{
    public sealed record GetOnboardingItSetupQuery(int Id) : IRequest<ProcessResult<OnboardingViewModel>>;
}

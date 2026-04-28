// Project comment: GetOnboardingItStep4Query.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep4
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingItStep4 query.
    /// </summary>
    public sealed record GetOnboardingItStep4Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep4Result>;
}

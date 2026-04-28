// Project comment: GetOnboardingItStep3Query.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep3
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingItStep3 query.
    /// </summary>
    public sealed record GetOnboardingItStep3Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep3Result>;
}

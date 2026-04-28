// Project comment: GetOnboardingItStep5Query.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItStep5
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingItStep5 query.
    /// </summary>
    public sealed record GetOnboardingItStep5Query(OnboardingViewModel SessionModel) : IRequest<GetOnboardingItStep5Result>;
}

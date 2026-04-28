// Project comment: GetOnboardingItSetupQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingItSetup
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingItSetup query.
    /// </summary>
    public sealed record GetOnboardingItSetupQuery(int Id) : IRequest<ProcessResult<OnboardingViewModel>>;
}

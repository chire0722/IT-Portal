// Project comment: GetOnboardingEditOptionsQuery.cs is part of the IT Form application source code.

using IT_Form.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEditOptions
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingEditOptions query.
    /// </summary>
    public sealed record GetOnboardingEditOptionsQuery(
        OnboardingRequest Model) : IRequest<EditOnboardingPageViewModel>;
}

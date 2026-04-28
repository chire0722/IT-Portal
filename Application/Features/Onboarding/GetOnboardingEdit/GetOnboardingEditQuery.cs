// Project comment: GetOnboardingEditQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingEdit
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingEdit query.
    /// </summary>
    public sealed record GetOnboardingEditQuery(int Id) : IRequest<ProcessResult<EditOnboardingPageViewModel>>;
}

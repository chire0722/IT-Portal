// Project comment: GetOnboardingDeleteQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDelete
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingDelete query.
    /// </summary>
    public sealed record GetOnboardingDeleteQuery(int Id) : IRequest<ProcessResult<OnboardingRequest>>;
}

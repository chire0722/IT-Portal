// Project comment: GetOnboardingDetailsQuery.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingDetails
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingDetails query.
    /// </summary>
    public sealed record GetOnboardingDetailsQuery(int Id) : IRequest<GetOnboardingDetailsResult>;
}

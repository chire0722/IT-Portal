// Project comment: GetOnboardingIndexQuery.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    /// <summary>
    /// Carries data needed to execute the GetOnboardingIndex query.
    /// </summary>
    public sealed record GetOnboardingIndexQuery(
        string? SearchString,
        RequestStatus? Status,
        int Page = 1) : IRequest<GetOnboardingIndexResult>;
}

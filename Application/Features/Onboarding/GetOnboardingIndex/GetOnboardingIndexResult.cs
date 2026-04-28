// Project comment: GetOnboardingIndexResult.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    /// <summary>
    /// Represents the result returned by the GetOnboardingIndex workflow.
    /// </summary>
    public sealed record GetOnboardingIndexResult(
        PaginatedList<OnboardingIndexListItem> Requests,
        string? CurrentFilter,
        RequestStatus? StatusFilter,
        int SlaTargetDays);
}

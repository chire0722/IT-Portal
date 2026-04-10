using IT_Form.Helpers;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    public sealed record GetOnboardingIndexResult(
        PaginatedList<OnboardingIndexListItem> Requests,
        string? CurrentFilter,
        RequestStatus? StatusFilter,
        int SlaTargetDays);
}

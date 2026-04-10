using IT_Form.Helpers;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Models.Enums;

namespace IT_Form.ViewModels
{
    public sealed class OffboardingIndexPageViewModel
    {
        public PaginatedList<OffboardingIndexListItem> Requests { get; init; } = null!;
        public string? CurrentFilter { get; init; }
        public RequestStatus? StatusFilter { get; init; }
        public int SlaTargetDays { get; init; }
    }
}

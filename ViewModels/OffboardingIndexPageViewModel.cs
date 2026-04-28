// Project comment: OffboardingIndexPageViewModel.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Models.Enums;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OffboardingIndexPage screen or form.
    /// </summary>
    public sealed class OffboardingIndexPageViewModel
    {
        public PaginatedList<OffboardingIndexListItem> Requests { get; init; } = null!;
        public string? CurrentFilter { get; init; }
        public RequestStatus? StatusFilter { get; init; }
        public int SlaTargetDays { get; init; }
    }
}

// Project comment: OnboardingIndexListItem.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    /// <summary>
    /// Defines the OnboardingIndexListItem class used by the IT Form application.
    /// </summary>
    public sealed class OnboardingIndexListItem
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? EmployeeId { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Branch { get; set; }
        public string CompanyTimeZoneId { get; set; } = CompanyTimeZoneHelper.DefaultTimeZoneId;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? RequesterSignedAt { get; set; }
        public DateTime? SlaDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

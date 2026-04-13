using IT_Form.Models.Enums;
using IT_Form.Helpers;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingIndex
{
    public sealed class OffboardingIndexListItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string Department { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string CompanyTimeZoneId { get; set; } = CompanyTimeZoneHelper.DefaultTimeZoneId;
        public string? HodEmail { get; set; }
        public string? AssignedHodName { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? HRSignedAt { get; set; }
        public DateTime? SlaDeadline { get; set; }
        public DateTime LastDayOfWork { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    public sealed class OnboardingIndexListItem
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? EmployeeId { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Branch { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}

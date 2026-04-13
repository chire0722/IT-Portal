using IT_Form.Models.Enums;

namespace IT_Form.Models
{
    public sealed class DashboardOnboardingQueueItem
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Branch { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? RequesterSignedAt { get; set; }
        public RequestStatus Status { get; set; }
    }

    public sealed class DashboardOffboardingQueueItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? HRSignedAt { get; set; }
        public DateTime LastDayOfWork { get; set; }
        public RequestStatus Status { get; set; }
    }

    public sealed class DashboardRequestSummaryItem
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
    }

    public sealed class DashboardLicenseAlertItem
    {
        public string SoftwareName { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
    }

    public sealed class DashboardAssetAlertItem
    {
        public string AssetTag { get; set; } = string.Empty;
        public DateTime? WarrantyExpiry { get; set; }
    }

    public sealed class DashboardAuditLogItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string? Details { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

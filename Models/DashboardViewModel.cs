using System.Collections.Generic;
using System.Linq;

namespace IT_Form.Models
{
    public class DashboardViewModel
    {
        public IEnumerable<DashboardOnboardingQueueItem> PendingOnboarding { get; set; } = new List<DashboardOnboardingQueueItem>();
        public IEnumerable<DashboardOffboardingQueueItem> PendingOffboarding { get; set; } = new List<DashboardOffboardingQueueItem>();

        // Overall Stats
        public int TotalOnboarding { get; set; }
        public int TotalOffboarding { get; set; }
        public int TotalAssets { get; set; }

        // Monthly Stats
        public int MonthlyOnboardingCount { get; set; }
        public int MonthlyOffboardingCount { get; set; }

        // Asset Distribution
        public Dictionary<Enums.AssetStatus, int> AssetDistribution { get; set; } = new Dictionary<Enums.AssetStatus, int>();

        // Expiring Items
        public IEnumerable<DashboardLicenseAlertItem> ExpiringLicenses { get; set; } = new List<DashboardLicenseAlertItem>();
        public IEnumerable<DashboardAssetAlertItem> ExpiringAssets { get; set; } = new List<DashboardAssetAlertItem>();

        // Recent Activity
        public IEnumerable<DashboardAuditLogItem> RecentActivity { get; set; } = new List<DashboardAuditLogItem>();

        // SLA Tracking
        public int OverdueOnboardingCount { get; set; }
        public int OverdueOffboardingCount { get; set; }
        public int OnboardingSlaDays { get; set; }
        public int OffboardingSlaDays { get; set; }

        // Count for badges
        public int TotalPending => PendingOnboarding.Count() + PendingOffboarding.Count();

        // ── HR Dashboard (pending queue) ──
        public IEnumerable<DashboardRequestSummaryItem> PendingEmployeeSignOnboarding { get; set; } = new List<DashboardRequestSummaryItem>();
        public IEnumerable<DashboardRequestSummaryItem> PendingEmployeeSignOffboarding { get; set; } = new List<DashboardRequestSummaryItem>();
        public IEnumerable<DashboardRequestSummaryItem> RecentCompletedOnboarding { get; set; } = new List<DashboardRequestSummaryItem>();
        public IEnumerable<DashboardRequestSummaryItem> RecentCompletedOffboarding { get; set; } = new List<DashboardRequestSummaryItem>();
        public int PendingEmployeeSignCount { get; set; }
        public int CompletedThisMonthCount { get; set; }

        // Monthly trend data
        public List<string> MonthlyTrendLabels { get; set; } = new List<string>();
        public List<int> MonthlyTrendOnboarding { get; set; } = new List<int>();
        public List<int> MonthlyTrendOffboarding { get; set; } = new List<int>();

        // ── IT Admin System Health Stats ──
        public int TotalActiveUsers { get; set; }
        public int TotalAuditLogEntries { get; set; }
        public int TotalCommandAuditEntries { get; set; }
        public int FailedCommandCount { get; set; }
        public int FailedCommandCountPeriod { get; set; }
        public IEnumerable<DashboardAuditLogItem> RecentFailedCommands { get; set; } = new List<DashboardAuditLogItem>();
        public int RejectedThisMonth { get; set; }
        public int PendingITApprovalCount { get; set; }

        // ── Enhanced HR Analytics ──
        public List<int> MonthlyNetGrowth { get; set; } = new List<int>();
        public double AvgOnboardingDays { get; set; }
        public double AvgOffboardingDays { get; set; }
        public double OnboardingRejectRate { get; set; }
        public double OffboardingRejectRate { get; set; }
        public Dictionary<string, int> TopDesignations { get; set; } = new Dictionary<string, int>();

        // Breakdowns by Department
        public Dictionary<string, int> DepartmentOnboardingBreakdown { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DepartmentOffboardingBreakdown { get; set; } = new Dictionary<string, int>();

        // Breakdowns by Branch / Company
        public Dictionary<string, int> BranchOnboardingBreakdown { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> BranchOffboardingBreakdown { get; set; } = new Dictionary<string, int>();

        // Status distribution (for donut chart)
        public Dictionary<string, int> OnboardingStatusDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> OffboardingStatusDistribution { get; set; } = new Dictionary<string, int>();

        // KPI counts
        public int PendingEmployeeSignOnboardingCount { get; set; }
        public int PendingEmployeeSignOffboardingCount { get; set; }
        public int TotalExitsYear { get; set; }

        // Avg tenure in days for employees who left this year (JoinDate → LastDayOfWork)
        public double AvgTenureDays { get; set; }

        // ── Enhanced IT Analytics ──
        public Dictionary<string, int> AssetTypeDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AssetStatusBreakdown { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> WindowsVersionDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SoftwareLicenseBreakdown { get; set; } = new Dictionary<string, int>();
        public List<SoftwareUtilizationViewModel> LicenseUtilization { get; set; } = new List<SoftwareUtilizationViewModel>();

        public int ExpiredWarrantyCount { get; set; }
        public int ExpiringWithin30Days { get; set; }
        public int ExpiringWithin90Days { get; set; }
        public int TotalSoftwareLicenses { get; set; }
        public int AssignedSoftwareLicenses { get; set; }
        public int TotalLicenses { get; set; } // Compatibility with legacy dashboard card

        // Distribution data for analytics
        public Dictionary<string, int> CpuDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RamDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> StorageDistribution { get; set; } = new Dictionary<string, int>();


        // Maintenance count for the selected year
        public int MaintenanceCountYear { get; set; }

        // ── Filtering ──
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new List<int>();

        // ── Unified IT Task Dashboard ──
        public List<CombinedPendingTask> UnifiedTasks { get; set; } = new List<CombinedPendingTask>();

        // ── SLA Bottleneck Analytics ──
        public double AvgHrPhaseDays { get; set; }
        public double AvgItPhaseDays { get; set; }
        public double AvgEmployeePhaseDays { get; set; }
        public Dictionary<string, int> SlaComplianceDistribution { get; set; } = new Dictionary<string, int>();

        // ── Year-over-Year (YoY) Comparisons ──
        public int PrevYearOnboardingCount { get; set; }
        public int PrevYearOffboardingCount { get; set; }
        public int PrevYearTotalAssets { get; set; }
        public double PrevYearAvgOnboardingDays { get; set; }
        
        // Calculated YoYs (helpers)
        public double OnboardingYoYChange => PrevYearOnboardingCount == 0 ? 0 : Math.Round(((double)(MonthlyOnboardingCount - PrevYearOnboardingCount) / PrevYearOnboardingCount) * 100, 1);
        public double OffboardingYoYChange => PrevYearOffboardingCount == 0 ? 0 : Math.Round(((double)(MonthlyOffboardingCount - PrevYearOffboardingCount) / PrevYearOffboardingCount) * 100, 1);
        public int AssetYoYDiff => TotalAssets - PrevYearTotalAssets;
        public double SlaYoYDiff => Math.Round(AvgOnboardingDays - PrevYearAvgOnboardingDays, 1);
    }

    public class CombinedPendingTask
    {
        public int Id { get; set; }
        public string RequestType { get; set; } = string.Empty; // Onboarding or Offboarding
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public IT_Form.Models.Enums.RequestStatus Status { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysPending { get; set; }
    }

    public class SoftwareUtilizationViewModel
    {
        public string SoftwareName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Used { get; set; }
        public double Percent { get; set; }
    }
}

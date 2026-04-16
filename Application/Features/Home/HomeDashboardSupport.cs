using System.Globalization;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Home;

internal static class HomeDashboardSupport
{
    public static DashboardPeriodContext CreatePeriodContext(int? year)
    {
        var currentYear = DateTime.UtcNow.Year;
        var selectedYear = year ?? currentYear;
        var isCurrentYear = selectedYear == currentYear;
        var yearStart = new DateTime(selectedYear, 1, 1);
        var yearEnd = new DateTime(selectedYear, 12, 31, 23, 59, 59);
        var statsStartDate = isCurrentYear
            ? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            : yearStart;
        var statsEndDate = isCurrentYear
            ? DateTime.UtcNow
            : yearEnd;

        return new DashboardPeriodContext(
            selectedYear,
            isCurrentYear,
            statsStartDate,
            statsEndDate,
            yearStart,
            yearEnd,
            Enumerable.Range(2024, Math.Max(currentYear - 2024, 0) + 1)
                .OrderByDescending(value => value)
                .ToList(),
            isCurrentYear ? "This Month" : $"In {selectedYear}");
    }

    public static void ApplyPeriodContext(DashboardViewModel viewModel, DashboardPeriodContext context)
    {
        viewModel.SelectedYear = context.SelectedYear;
        viewModel.AvailableYears = context.AvailableYears.ToList();
    }

    public static async Task<MonthlyTrendResult> BuildMonthlyTrendAsync(
        ApplicationDbContext context,
        DashboardPeriodContext period,
        CancellationToken cancellationToken)
        => await BuildMonthlyTrendAsync(
            context.OnboardingRequests.AsNoTracking(),
            context.OffboardingRequests.AsNoTracking(),
            period,
            cancellationToken);

    public static async Task<MonthlyTrendResult> BuildMonthlyTrendAsync(
        IQueryable<OnboardingRequest> onboardingQuery,
        IQueryable<OffboardingRequest> offboardingQuery,
        DashboardPeriodContext period,
        CancellationToken cancellationToken)
    {
        var onboardingCounts = await onboardingQuery
            .Where(request => request.RequestDate >= period.YearStart && request.RequestDate <= period.YearEnd)
            .GroupBy(request => request.RequestDate.Month)
            .Select(group => new
            {
                Month = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Month, item => item.Count, cancellationToken);

        var offboardingCounts = await offboardingQuery
            .Where(request => request.RequestDate >= period.YearStart && request.RequestDate <= period.YearEnd)
            .GroupBy(request => request.RequestDate.Month)
            .Select(group => new
            {
                Month = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Month, item => item.Count, cancellationToken);

        var labels = new List<string>(12);
        var onboarding = new List<int>(12);
        var offboarding = new List<int>(12);

        for (var month = 1; month <= 12; month++)
        {
            labels.Add(new DateTime(period.SelectedYear, month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture));
            onboarding.Add(onboardingCounts.GetValueOrDefault(month));
            offboarding.Add(offboardingCounts.GetValueOrDefault(month));
        }

        return new MonthlyTrendResult(labels, onboarding, offboarding);
    }

    public static async Task PopulateAuditMetricsAsync(
        ApplicationDbContext context,
        DashboardViewModel viewModel,
        DateTime statsStartDate,
        DateTime statsEndDate,
        CancellationToken cancellationToken)
    {
        viewModel.TotalAuditLogEntries = await context.AuditLogs
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var commandAuditQuery = context.AuditLogs
            .AsNoTracking()
            .Where(audit => audit.EntityName == "Command");

        var commandAuditSummary = await commandAuditQuery
            .GroupBy(static _ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Failed = group.Count(audit => !audit.IsSuccess),
                FailedInPeriod = group.Count(audit =>
                    !audit.IsSuccess &&
                    audit.CreatedAt >= statsStartDate &&
                    audit.CreatedAt <= statsEndDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        viewModel.TotalCommandAuditEntries = commandAuditSummary?.Total ?? 0;
        viewModel.FailedCommandCount = commandAuditSummary?.Failed ?? 0;
        viewModel.FailedCommandCountPeriod = commandAuditSummary?.FailedInPeriod ?? 0;

        viewModel.RecentFailedCommands = await commandAuditQuery
            .Where(audit => !audit.IsSuccess)
            .OrderByDescending(audit => audit.CreatedAt)
            .ThenByDescending(audit => audit.Timestamp)
            .Take(5)
            .ProjectToType<DashboardAuditLogItem>()
            .ToListAsync(cancellationToken);
    }

    public static async Task<Dictionary<string, int>> BuildWindowsVersionDistributionAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
        => await BuildWindowsVersionDistributionAsync(
            context.ITAssets.AsNoTracking(),
            cancellationToken);

    public static async Task<Dictionary<string, int>> BuildWindowsVersionDistributionAsync(
        IQueryable<IT_Form.Models.ITAsset> assetQuery,
        CancellationToken cancellationToken)
    {
        var windowsVersions = await assetQuery
            .Where(asset => !string.IsNullOrWhiteSpace(asset.WindowsVersion))
            .Select(asset => asset.WindowsVersion!)
            .ToListAsync(cancellationToken);

        return windowsVersions
            .GroupBy(NormalizeOS)
            .Select(group => new
            {
                Version = group.Key,
                Count = group.Count()
            })
            .ToDictionary(item => item.Version, item => item.Count);
    }

    public static async Task<LicenseUtilizationResult> BuildLicenseUtilizationAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
        => await BuildLicenseUtilizationAsync(
            context.SoftwareLicenses.AsNoTracking(),
            cancellationToken);

    public static async Task<LicenseUtilizationResult> BuildLicenseUtilizationAsync(
        IQueryable<SoftwareLicense> softwareLicensesQuery,
        CancellationToken cancellationToken)
    {
        var softwareLicenses = await softwareLicensesQuery
            .Select(license => new SoftwareLicenseUsageSnapshot(
                license.SoftwareName,
                license.IsFree,
                license.IsSubscription,
                license.ContractLimit,
                license.Keys.Count(),
                license.Keys.Count(key => key.IsAssigned)))
            .ToListAsync(cancellationToken);

        var items = softwareLicenses
            .Select(license =>
            {
                var total = GetLicenseSeatCapacity(license);
                var used = GetAssignedSeatCount(license);

                return new SoftwareUtilizationViewModel
                {
                    SoftwareName = license.SoftwareName,
                    IsUnlimited = license.IsFree,
                    Total = total,
                    Used = used,
                    Percent = total > 0
                        ? Math.Round((double)used / total * 100, 1)
                        : 0
                };
            })
            .OrderByDescending(item => item.Percent)
            .ToList();

        return new LicenseUtilizationResult(
            items,
            items.Sum(item => item.Total),
            items.Sum(item => item.Used));
    }

    public static int GetLicenseSeatCapacity(SoftwareLicense license)
    {
        if (license.IsFree)
        {
            return 0;
        }

        return license.IsSubscription
            ? Math.Max(license.ContractLimit, 0)
            : license.Keys.Count;
    }

    public static int GetAssignedSeatCount(SoftwareLicense license)
    {
        if (license.IsFree)
        {
            return 0;
        }

        return license.Keys.Count(key => key.IsAssigned);
    }

    private static int GetLicenseSeatCapacity(SoftwareLicenseUsageSnapshot license)
    {
        if (license.IsFree)
        {
            return 0;
        }

        return license.IsSubscription
            ? Math.Max(license.ContractLimit, 0)
            : license.KeyCount;
    }

    private static int GetAssignedSeatCount(SoftwareLicenseUsageSnapshot license)
    {
        if (license.IsFree)
        {
            return 0;
        }

        return license.AssignedKeyCount;
    }

    public static string NormalizeOS(string? os)
    {
        if (string.IsNullOrWhiteSpace(os))
        {
            return "Unknown";
        }

        var normalized = os.Trim().ToLowerInvariant();

        if (normalized.Contains("win") && normalized.Contains("10") && normalized.Contains("pro"))
        {
            return "Windows 10 Pro";
        }

        if (normalized.Contains("win") && normalized.Contains("11") && normalized.Contains("pro"))
        {
            return "Windows 11 Pro";
        }

        if (normalized.Contains("win") && normalized.Contains("10") && normalized.Contains("ent"))
        {
            return "Windows 10 Enterprise";
        }

        if (normalized.Contains("win") && normalized.Contains("11") && normalized.Contains("ent"))
        {
            return "Windows 11 Enterprise";
        }

        if (normalized.Contains("win") && normalized.Contains("10"))
        {
            return "Windows 10";
        }

        if (normalized.Contains("win") && normalized.Contains("11"))
        {
            return "Windows 11";
        }

        if (normalized.Contains("mac"))
        {
            return "macOS";
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }

    private sealed record SoftwareLicenseUsageSnapshot(
        string SoftwareName,
        bool IsFree,
        bool IsSubscription,
        int ContractLimit,
        int KeyCount,
        int AssignedKeyCount);
}

// Project comment: HomePageQueries.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Security;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IT_Form.Application.Features.Home;

/// <summary>
/// Handles the GetHomeDashboardQuery query workflow.
/// </summary>
public sealed class GetHomeDashboardQueryHandler : IRequestHandler<GetHomeDashboardQuery, HomePageResult>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IMemoryCache _cache;

    public GetHomeDashboardQueryHandler(
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor,
        IMemoryCache cache)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _cache = cache;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<HomePageResult> Handle(GetHomeDashboardQuery request, CancellationToken cancellationToken)
    {
        var user = _currentUserAccessor.User;
        var currentUserName = _currentUserAccessor.UserName ?? string.Empty;
        var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var hasCurrentUserId = !string.IsNullOrWhiteSpace(currentUserId);
        var hodEmails = await UserHelper.GetHODEmailsAsync(_context, currentUserName);
        var isIt = user.HasClaim("UserTitle", "IT");
        var isManager = user.HasClaim("UserTitle", "Manager") || user.HasClaim("UserTitle", "HOD");
        var isHr = user.HasClaim("UserTitle", "HR");
        var itScopedCompanies = isIt && !isHr
            ? CompanyScopeHelper.GetScopedCompanies(user)
            : [];
        var cacheKey = BuildCacheKey(currentUserId, currentUserName, request.Year, isIt, isManager, isHr);

        if (_cache.TryGetValue<HomePageResult>(cacheKey, out var cachedResult) && cachedResult is not null)
        {
            return cachedResult;
        }

        var viewModel = new DashboardViewModel();
        var period = HomeDashboardSupport.CreatePeriodContext(request.Year);
        HomeDashboardSupport.ApplyPeriodContext(viewModel, period);

        IQueryable<OnboardingRequest> onboardingQuery = _context.OnboardingRequests
            .AsNoTracking()
            .AsQueryable();

        if (isIt || isHr)
        {
            onboardingQuery = onboardingQuery.Where(onboarding =>
                onboarding.Status != RequestStatus.Completed &&
                onboarding.Status != RequestStatus.Rejected &&
                onboarding.Status != RequestStatus.PendingEmployeeSign);

            if (isIt)
            {
                onboardingQuery = onboardingQuery.Where(onboarding => onboarding.Status != RequestStatus.Draft);
            }

            if (itScopedCompanies.Count > 0)
            {
                onboardingQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(onboardingQuery, itScopedCompanies);
            }
        }
        else
        {
            onboardingQuery = onboardingQuery.Where(static _ => false);
        }

        viewModel.PendingOnboarding = await onboardingQuery
            .OrderByDescending(onboarding => onboarding.RequestDate)
            .ProjectToType<DashboardOnboardingQueueItem>()
            .ToListAsync(cancellationToken);

        IQueryable<OffboardingRequest> offboardingQuery = _context.OffboardingRequests
            .AsNoTracking()
            .AsQueryable();

        if (isHr)
        {
            offboardingQuery = offboardingQuery.Where(offboarding =>
                offboarding.Status != RequestStatus.Completed &&
                offboarding.Status != RequestStatus.Rejected &&
                offboarding.Status != RequestStatus.PendingEmployeeSign);
        }
        else
        {
            offboardingQuery = offboardingQuery.Where(offboarding =>
                (isIt && offboarding.Status == RequestStatus.PendingITApproval) ||
                (isManager &&
                 (offboarding.Status == RequestStatus.PendingHODApproval ||
                  offboarding.Status == RequestStatus.PendingApproval ||
                  (offboarding.Status == RequestStatus.Draft &&
                   hasCurrentUserId &&
                   offboarding.AssignedHodId == currentUserId)) &&
                 string.IsNullOrEmpty(offboarding.HODSignature) &&
                 ((hasCurrentUserId && offboarding.AssignedHodId == currentUserId) ||
                  (offboarding.AssignedHodId == null &&
                   offboarding.HODEmail != null &&
                   hodEmails.Contains(offboarding.HODEmail.ToLower())))));

            if (itScopedCompanies.Count > 0)
            {
                offboardingQuery = CompanyScopeHelper.ApplyCompanyScope(
                    offboardingQuery,
                    itScopedCompanies,
                    offboarding => offboarding.Branch);
            }
        }

        viewModel.PendingOffboarding = await offboardingQuery
            .OrderByDescending(offboarding => offboarding.RequestDate)
            .ProjectToType<DashboardOffboardingQueueItem>()
            .ToListAsync(cancellationToken);
        var companies = await _context.Companies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var slaSettings = await _context.SlaSettings
            .AsNoTracking()
            .Where(setting =>
                setting.RequestType == "Onboarding" ||
                setting.RequestType == "Offboarding")
            .ToListAsync(cancellationToken);

        var onboardingSlaDays = slaSettings
            .FirstOrDefault(setting => setting.RequestType == "Onboarding")
            ?.TargetDays ?? 3;
        var offboardingSlaDays = slaSettings
            .FirstOrDefault(setting => setting.RequestType == "Offboarding")
            ?.TargetDays ?? 3;

        if (isIt)
        {
            var unifiedTasks = new List<CombinedPendingTask>();

            foreach (var onboarding in viewModel.PendingOnboarding.Where(request => request.Status == RequestStatus.PendingITApproval))
            {
                var slaStart = onboarding.RequesterSignedAt ?? onboarding.RequestDate;
                unifiedTasks.Add(new CombinedPendingTask
                {
                    Id = onboarding.Id,
                    RequestType = "Onboarding",
                    EmployeeName = onboarding.FullName ?? "Unknown",
                    Department = onboarding.Department ?? "Unknown",
                    RequestDate = slaStart,
                    Status = onboarding.Status,
                    DaysPending = Math.Max((DateTime.UtcNow - slaStart).Days, 0),
                    IsOverdue = CompanyBusinessHoursHelper.IsSlaOverdue(
                        companies,
                        onboarding.Branch,
                        slaStart,
                        onboardingSlaDays)
                });
            }

            foreach (var offboarding in viewModel.PendingOffboarding.Where(request => request.Status == RequestStatus.PendingITApproval))
            {
                var slaStart = offboarding.HRSignedAt ?? offboarding.RequestDate;
                unifiedTasks.Add(new CombinedPendingTask
                {
                    Id = offboarding.Id,
                    RequestType = "Offboarding",
                    EmployeeName = offboarding.FullName ?? "Unknown",
                    Department = offboarding.Department ?? "Unknown",
                    RequestDate = slaStart,
                    Status = offboarding.Status,
                    DaysPending = Math.Max((DateTime.UtcNow - slaStart).Days, 0),
                    IsOverdue = CompanyBusinessHoursHelper.IsSlaOverdue(
                        companies,
                        offboarding.Branch,
                        slaStart,
                        offboardingSlaDays)
                });
            }

            viewModel.UnifiedTasks = unifiedTasks
                .OrderByDescending(task => task.IsOverdue)
                .ThenByDescending(task => task.DaysPending)
                .ToList();
        }

        viewModel.OnboardingSlaDays = onboardingSlaDays;
        viewModel.OffboardingSlaDays = offboardingSlaDays;
        viewModel.OverdueOnboardingCount = viewModel.PendingOnboarding
            .Count(onboarding => CompanyBusinessHoursHelper.IsSlaOverdue(
                companies,
                onboarding.Branch,
                onboarding.RequesterSignedAt ?? onboarding.RequestDate,
                onboardingSlaDays));
        viewModel.OverdueOffboardingCount = viewModel.PendingOffboarding
            .Count(offboarding => CompanyBusinessHoursHelper.IsSlaOverdue(
                companies,
                offboarding.Branch,
                offboarding.HRSignedAt ?? offboarding.RequestDate,
                offboardingSlaDays));

        if (isIt || isHr)
        {
            viewModel.TotalOnboarding = await _context.OnboardingRequests
                .AsNoTracking()
                .CountAsync(cancellationToken);
            viewModel.TotalOffboarding = await _context.OffboardingRequests
                .AsNoTracking()
                .CountAsync(cancellationToken);
            viewModel.TotalAssets = await _context.ITAssets
                .AsNoTracking()
                .CountAsync(cancellationToken);
            viewModel.AssetDistribution = await _context.ITAssets
                .AsNoTracking()
                .GroupBy(asset => asset.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
            viewModel.MonthlyOnboardingCount = await _context.OnboardingRequests
                .AsNoTracking()
                .CountAsync(onboarding =>
                    onboarding.RequestDate >= period.StatsStartDate &&
                    onboarding.RequestDate <= period.StatsEndDate,
                    cancellationToken);
            viewModel.MonthlyOffboardingCount = await _context.OffboardingRequests
                .AsNoTracking()
                .CountAsync(offboarding =>
                    offboarding.RequestDate >= period.StatsStartDate &&
                    offboarding.RequestDate <= period.StatsEndDate,
                    cancellationToken);

            var today = DateTime.UtcNow.Date;
            var thirtyDaysFromToday = today.AddDays(30);

            viewModel.ExpiringLicenses = await _context.SoftwareLicenses
                .AsNoTracking()
                .Where(license =>
                    !license.IsDeleted &&
                    license.IsSubscription &&
                    license.ExpiryDate.HasValue &&
                    license.ExpiryDate <= thirtyDaysFromToday)
                .OrderBy(license => license.ExpiryDate)
                .Take(10)
                .ProjectToType<DashboardLicenseAlertItem>()
                .ToListAsync(cancellationToken);

            viewModel.ExpiringAssets = await _context.ITAssets
                .AsNoTracking()
                .Where(asset =>
                    !asset.IsDeleted &&
                    !asset.NoExtendWarranty &&
                    asset.WarrantyExpiry.HasValue &&
                    asset.WarrantyExpiry <= thirtyDaysFromToday)
                .OrderBy(asset => asset.WarrantyExpiry)
                .Take(10)
                .ProjectToType<DashboardAssetAlertItem>()
                .ToListAsync(cancellationToken);

            viewModel.RecentActivity = await _context.AuditLogs
                .AsNoTracking()
                .OrderByDescending(audit => audit.CreatedAt)
                .ThenByDescending(audit => audit.Timestamp)
                .Take(10)
                .ProjectToType<DashboardAuditLogItem>()
                .ToListAsync(cancellationToken);

            await HomeDashboardSupport.PopulateAuditMetricsAsync(
                _context,
                viewModel,
                period.StatsStartDate,
                period.StatsEndDate,
                cancellationToken);

            var licenseUtilization = await HomeDashboardSupport.BuildLicenseUtilizationAsync(_context, cancellationToken);
            viewModel.TotalSoftwareLicenses = licenseUtilization.TotalLicenses;
            viewModel.AssignedSoftwareLicenses = licenseUtilization.AssignedLicenses;
            viewModel.SoftwareLicenseBreakdown = new Dictionary<string, int>
            {
                { "Assigned", licenseUtilization.AssignedLicenses },
                { "Available", Math.Max(licenseUtilization.TotalLicenses - licenseUtilization.AssignedLicenses, 0) }
            };
        }

        if (isIt)
        {
            viewModel.TotalActiveUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(userEntry => userEntry.IsActive, cancellationToken);
            viewModel.RejectedThisMonth =
                await _context.OnboardingRequests
                    .AsNoTracking()
                    .CountAsync(onboarding =>
                        onboarding.Status == RequestStatus.Rejected &&
                        onboarding.UpdatedAt >= period.StatsStartDate &&
                        onboarding.UpdatedAt <= period.StatsEndDate,
                        cancellationToken)
                + await _context.OffboardingRequests
                    .AsNoTracking()
                    .CountAsync(offboarding =>
                        offboarding.Status == RequestStatus.Rejected &&
                        offboarding.UpdatedAt >= period.StatsStartDate &&
                        offboarding.UpdatedAt <= period.StatsEndDate,
                        cancellationToken);
            viewModel.PendingITApprovalCount =
                await _context.OnboardingRequests
                    .AsNoTracking()
                    .CountAsync(onboarding => onboarding.Status == RequestStatus.PendingITApproval, cancellationToken)
                + await _context.OffboardingRequests
                    .AsNoTracking()
                    .CountAsync(offboarding => offboarding.Status == RequestStatus.PendingITApproval, cancellationToken);
        }

        if (isHr || isIt)
        {
            var pendingEmployeeSignOnboardingQuery = _context.OnboardingRequests
                .AsNoTracking()
                .Where(onboarding => onboarding.Status == RequestStatus.PendingEmployeeSign)
                .AsQueryable();
            if (itScopedCompanies.Count > 0)
            {
                pendingEmployeeSignOnboardingQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(
                    pendingEmployeeSignOnboardingQuery,
                    itScopedCompanies);
            }

            viewModel.PendingEmployeeSignOnboarding = await pendingEmployeeSignOnboardingQuery
                .OrderByDescending(onboarding => onboarding.CompletedAt)
                .ProjectToType<DashboardRequestSummaryItem>()
                .ToListAsync(cancellationToken);

            var pendingEmployeeSignOffboardingQuery = _context.OffboardingRequests
                .AsNoTracking()
                .Where(offboarding => offboarding.Status == RequestStatus.PendingEmployeeSign)
                .AsQueryable();
            if (itScopedCompanies.Count > 0)
            {
                pendingEmployeeSignOffboardingQuery = CompanyScopeHelper.ApplyCompanyScope(
                    pendingEmployeeSignOffboardingQuery,
                    itScopedCompanies,
                    offboarding => offboarding.Branch);
            }

            viewModel.PendingEmployeeSignOffboarding = await pendingEmployeeSignOffboardingQuery
                .OrderByDescending(offboarding => offboarding.CompletedAt)
                .ProjectToType<DashboardRequestSummaryItem>()
                .ToListAsync(cancellationToken);
            viewModel.PendingEmployeeSignCount = viewModel.PendingEmployeeSignOnboarding.Count() +
                                                 viewModel.PendingEmployeeSignOffboarding.Count();
        }

        if (isHr || isIt)
        {
            viewModel.RecentCompletedOnboarding = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(onboarding => onboarding.Status == RequestStatus.Completed)
                .OrderByDescending(onboarding => onboarding.EmployeeSignedAt)
                .Take(5)
                .ProjectToType<DashboardRequestSummaryItem>()
                .ToListAsync(cancellationToken);
            viewModel.RecentCompletedOffboarding = await _context.OffboardingRequests
                .AsNoTracking()
                .Where(offboarding => offboarding.Status == RequestStatus.Completed)
                .OrderByDescending(offboarding => offboarding.EmployeeSignedAt)
                .Take(5)
                .ProjectToType<DashboardRequestSummaryItem>()
                .ToListAsync(cancellationToken);
            viewModel.CompletedThisMonthCount =
                await _context.OnboardingRequests
                    .AsNoTracking()
                    .CountAsync(onboarding =>
                        onboarding.Status == RequestStatus.Completed &&
                        onboarding.EmployeeSignedAt >= period.StatsStartDate &&
                        onboarding.EmployeeSignedAt <= period.StatsEndDate,
                        cancellationToken)
                + await _context.OffboardingRequests
                    .AsNoTracking()
                    .CountAsync(offboarding =>
                        offboarding.Status == RequestStatus.Completed &&
                        offboarding.EmployeeSignedAt >= period.StatsStartDate &&
                        offboarding.EmployeeSignedAt <= period.StatsEndDate,
                        cancellationToken);
            viewModel.DepartmentOnboardingBreakdown = await _context.OnboardingRequests
                .AsNoTracking()
                .Where(onboarding =>
                    onboarding.RequestDate >= period.YearStart &&
                    onboarding.RequestDate <= period.YearEnd &&
                    onboarding.Department != null &&
                    onboarding.Department != string.Empty)
                .GroupBy(onboarding => onboarding.Department!)
                .Select(group => new
                {
                    Department = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(item => item.Department, item => item.Count, cancellationToken);
            viewModel.DepartmentOffboardingBreakdown = await _context.OffboardingRequests
                .AsNoTracking()
                .Where(offboarding =>
                    offboarding.RequestDate >= period.YearStart &&
                    offboarding.RequestDate <= period.YearEnd &&
                    offboarding.Department != null &&
                    offboarding.Department != string.Empty)
                .GroupBy(offboarding => offboarding.Department!)
                .Select(group => new
                {
                    Department = group.Key,
                    Count = group.Count()
                })
                .ToDictionaryAsync(item => item.Department, item => item.Count, cancellationToken);

            var trend = await HomeDashboardSupport.BuildMonthlyTrendAsync(_context, period, cancellationToken);
            viewModel.MonthlyTrendLabels = trend.Labels;
            viewModel.MonthlyTrendOnboarding = trend.Onboarding;
            viewModel.MonthlyTrendOffboarding = trend.Offboarding;
        }

        var result = new HomePageResult(
            viewModel,
            period.IsCurrentYear,
            period.StatsPeriodLabel,
            Array.Empty<PageCompanyOption>(),
            null,
            null);
        _cache.Set(
            cacheKey,
            result,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
            });

        return result;
    }

    /// <summary>
    /// Builds the value returned by the BuildCacheKey operation.
    /// </summary>
    private static string BuildCacheKey(
        string? currentUserId,
        string currentUserName,
        int? year,
        bool isIt,
        bool isManager,
        bool isHr)
        => string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"home-dashboard:{currentUserId ?? currentUserName}:{year?.ToString() ?? "current"}:{isIt}:{isManager}:{isHr}");
}

/// <summary>
/// Handles the GetHomeAnalyticsQuery query workflow.
/// </summary>
public sealed class GetHomeAnalyticsQueryHandler : IRequestHandler<GetHomeAnalyticsQuery, ProcessResult<HomePageResult>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public GetHomeAnalyticsQueryHandler(
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<HomePageResult>> Handle(GetHomeAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var user = _currentUserAccessor.User;
        var isHr = user.HasClaim("UserTitle", PortalTitles.HR);
        var isSuperAdmin = PortalTitles.IsSuperAdmin(user);
        var isIt = PortalTitles.IsPrivilegedIt(user);

        if (!isHr && !isIt && !isSuperAdmin)
        {
            return ProcessResult<HomePageResult>.Forbidden();
        }

        var viewModel = new DashboardViewModel();
        var period = HomeDashboardSupport.CreatePeriodContext(request.Year);
        HomeDashboardSupport.ApplyPeriodContext(viewModel, period);
        var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
        var accessibleCompanyIds = scopedCompanies.Count > 0
            ? await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, user, cancellationToken, activeOnly: true)
            : null;
        var companyOptions = await PageQueryLookupSupport.LoadActiveCompaniesAsync(_context, user, cancellationToken);

        PageCompanyOption? selectedCompany = null;
        if (request.CompanyId.HasValue)
        {
            selectedCompany = companyOptions.FirstOrDefault(company => company.Id == request.CompanyId.Value);
            if (selectedCompany is null)
            {
                return ProcessResult<HomePageResult>.Forbidden();
            }
        }

        var companyDisplayFilters = selectedCompany is not null
            ? new List<string> { selectedCompany.DisplayName }
            : scopedCompanies;
        var selectedCompanyIds = selectedCompany is not null
            ? new HashSet<int> { selectedCompany.Id }
            : accessibleCompanyIds;

        IQueryable<OnboardingRequest> onboardingAnalyticsQuery = _context.OnboardingRequests
            .AsNoTracking();
        IQueryable<OffboardingRequest> offboardingAnalyticsQuery = _context.OffboardingRequests
            .AsNoTracking();
        IQueryable<IT_Form.Models.ITAsset> assetAnalyticsQuery = _context.ITAssets
            .AsNoTracking();
        IQueryable<MaintenanceLog> maintenanceAnalyticsQuery = _context.MaintenanceLogs
            .AsNoTracking();
        IQueryable<SoftwareLicense> licenseAnalyticsQuery = _context.SoftwareLicenses
            .AsNoTracking();

        if (companyDisplayFilters.Count > 0)
        {
            onboardingAnalyticsQuery = CompanyScopeHelper.ApplyOnboardingCompanyScope(onboardingAnalyticsQuery, companyDisplayFilters);
            offboardingAnalyticsQuery = CompanyScopeHelper.ApplyCompanyScope(offboardingAnalyticsQuery, companyDisplayFilters, offboarding => offboarding.Branch);
            assetAnalyticsQuery = CompanyScopeHelper.ApplyCompanyScope(assetAnalyticsQuery, companyDisplayFilters, asset => asset.Company);
            maintenanceAnalyticsQuery = CompanyScopeHelper.ApplyCompanyScope(maintenanceAnalyticsQuery, companyDisplayFilters, log => log.ITAsset!.Company);
        }

        if (selectedCompanyIds != null)
        {
            licenseAnalyticsQuery = licenseAnalyticsQuery.Where(license =>
                !license.SoftwareLicenseCompanies.Any() ||
                license.SoftwareLicenseCompanies.Any(link => selectedCompanyIds.Contains(link.CompanyId)));
        }

        var trend = await HomeDashboardSupport.BuildMonthlyTrendAsync(
            onboardingAnalyticsQuery,
            offboardingAnalyticsQuery,
            period,
            cancellationToken);
        viewModel.MonthlyTrendLabels = trend.Labels;
        viewModel.MonthlyTrendOnboarding = trend.Onboarding;
        viewModel.MonthlyTrendOffboarding = trend.Offboarding;
        viewModel.MonthlyNetGrowth = trend.Onboarding
            .Zip(trend.Offboarding, (onboarding, offboarding) => onboarding - offboarding)
            .ToList();

        viewModel.DepartmentOnboardingBreakdown = await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                onboarding.Department != null &&
                onboarding.Department != string.Empty)
            .GroupBy(onboarding => onboarding.Department!)
            .Select(group => new
            {
                Department = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Department, item => item.Count, cancellationToken);

        viewModel.DepartmentOffboardingBreakdown = await offboardingAnalyticsQuery
            .Where(offboarding =>
                offboarding.RequestDate >= period.YearStart &&
                offboarding.RequestDate <= period.YearEnd &&
                offboarding.Department != null &&
                offboarding.Department != string.Empty)
            .GroupBy(offboarding => offboarding.Department!)
            .Select(group => new
            {
                Department = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Department, item => item.Count, cancellationToken);

        viewModel.BranchOnboardingBreakdown = await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                onboarding.Branch != null &&
                onboarding.Branch != string.Empty)
            .GroupBy(onboarding => onboarding.Branch!)
            .Select(group => new
            {
                Branch = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Branch, item => item.Count, cancellationToken);

        viewModel.BranchOffboardingBreakdown = await offboardingAnalyticsQuery
            .Where(offboarding =>
                offboarding.RequestDate >= period.YearStart &&
                offboarding.RequestDate <= period.YearEnd &&
                offboarding.Branch != null &&
                offboarding.Branch != string.Empty)
            .GroupBy(offboarding => offboarding.Branch!)
            .Select(group => new
            {
                Branch = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.Branch, item => item.Count, cancellationToken);

        viewModel.OnboardingStatusDistribution = (await onboardingAnalyticsQuery
            .Where(onboarding => onboarding.RequestDate >= period.YearStart && onboarding.RequestDate <= period.YearEnd)
            .GroupBy(onboarding => onboarding.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Status.ToString(), item => item.Count);

        viewModel.OffboardingStatusDistribution = (await offboardingAnalyticsQuery
            .Where(offboarding => offboarding.RequestDate >= period.YearStart && offboarding.RequestDate <= period.YearEnd)
            .GroupBy(offboarding => offboarding.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Status.ToString(), item => item.Count);

        var completedOnboarding = await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                onboarding.CompletedAt.HasValue)
            .Select(onboarding => new
            {
                onboarding.RequestDate,
                onboarding.CompletedAt
            })
            .ToListAsync(cancellationToken);

        if (completedOnboarding.Count > 0)
        {
            viewModel.AvgOnboardingDays = Math.Round(
                completedOnboarding.Average(onboarding => (onboarding.CompletedAt!.Value - onboarding.RequestDate).TotalDays),
                1);
        }

        var completedOffboarding = await offboardingAnalyticsQuery
            .Where(offboarding =>
                offboarding.RequestDate >= period.YearStart &&
                offboarding.RequestDate <= period.YearEnd &&
                offboarding.CompletedAt.HasValue)
            .Select(offboarding => new
            {
                offboarding.RequestDate,
                offboarding.CompletedAt
            })
            .ToListAsync(cancellationToken);

        if (completedOffboarding.Count > 0)
        {
            viewModel.AvgOffboardingDays = Math.Round(
                completedOffboarding.Average(offboarding => (offboarding.CompletedAt!.Value - offboarding.RequestDate).TotalDays),
                1);
        }

        var totalOnboardingInYear = await onboardingAnalyticsQuery
            .CountAsync(onboarding => onboarding.RequestDate >= period.YearStart && onboarding.RequestDate <= period.YearEnd, cancellationToken);
        var rejectedOnboardingInYear = await onboardingAnalyticsQuery
            .CountAsync(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                onboarding.Status == RequestStatus.Rejected,
                cancellationToken);

        if (totalOnboardingInYear > 0)
        {
            viewModel.OnboardingRejectRate = Math.Round((double)rejectedOnboardingInYear / totalOnboardingInYear * 100, 1);
        }

        var totalOffboardingInYear = await offboardingAnalyticsQuery
            .CountAsync(offboarding => offboarding.RequestDate >= period.YearStart && offboarding.RequestDate <= period.YearEnd, cancellationToken);
        var rejectedOffboardingInYear = await offboardingAnalyticsQuery
            .CountAsync(offboarding =>
                offboarding.RequestDate >= period.YearStart &&
                offboarding.RequestDate <= period.YearEnd &&
                offboarding.Status == RequestStatus.Rejected,
                cancellationToken);

        if (totalOffboardingInYear > 0)
        {
            viewModel.OffboardingRejectRate = Math.Round((double)rejectedOffboardingInYear / totalOffboardingInYear * 100, 1);
        }

        viewModel.TopDesignations = (await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                !string.IsNullOrEmpty(onboarding.Designation))
            .GroupBy(onboarding => onboarding.Designation!)
            .Select(group => new
            {
                Designation = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Designation)
            .Take(5)
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Designation, item => item.Count);

        viewModel.TotalExitsYear = totalOffboardingInYear;
        viewModel.PendingEmployeeSignOnboardingCount = await onboardingAnalyticsQuery
            .CountAsync(onboarding => onboarding.Status == RequestStatus.PendingEmployeeSign, cancellationToken);
        viewModel.PendingEmployeeSignOffboardingCount = await offboardingAnalyticsQuery
            .CountAsync(offboarding => offboarding.Status == RequestStatus.PendingEmployeeSign, cancellationToken);
        viewModel.PendingEmployeeSignCount = viewModel.PendingEmployeeSignOnboardingCount + viewModel.PendingEmployeeSignOffboardingCount;

        var tenureData = await offboardingAnalyticsQuery
            .Where(offboarding =>
                offboarding.RequestDate >= period.YearStart &&
                offboarding.RequestDate <= period.YearEnd &&
                offboarding.OnboardingRequestId != null)
            .Select(offboarding => new
            {
                offboarding.OnboardingRequestId,
                offboarding.LastDayOfWork
            })
            .ToListAsync(cancellationToken);

        if (tenureData.Count > 0)
        {
            var onboardingIds = tenureData
                .Where(item => item.OnboardingRequestId.HasValue)
                .Select(item => item.OnboardingRequestId!.Value)
                .Distinct()
                .ToList();

            var joinDates = await onboardingAnalyticsQuery
                .Where(onboarding => onboardingIds.Contains(onboarding.Id) && onboarding.JoinDate.HasValue)
                .Select(onboarding => new
                {
                    onboarding.Id,
                    JoinDate = onboarding.JoinDate!.Value
                })
                .ToListAsync(cancellationToken);

            var joinDateLookup = joinDates.ToDictionary(item => item.Id, item => item.JoinDate);
            var tenureDays = tenureData
                .Where(item => item.OnboardingRequestId.HasValue && joinDateLookup.ContainsKey(item.OnboardingRequestId.Value))
                .Select(item => (item.LastDayOfWork - joinDateLookup[item.OnboardingRequestId!.Value]).TotalDays)
                .Where(days => days > 0)
                .ToList();

            if (tenureDays.Count > 0)
            {
                viewModel.AvgTenureDays = Math.Round(tenureDays.Average(), 0);
            }
        }

        viewModel.AssetTypeDistribution = (await assetAnalyticsQuery
            .GroupBy(asset => asset.AssetType)
            .Select(group => new
            {
                Type = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Type.ToString(), item => item.Count);

        viewModel.AssetStatusBreakdown = (await assetAnalyticsQuery
            .GroupBy(asset => asset.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Status.ToString(), item => item.Count);

        viewModel.WindowsVersionDistribution = await HomeDashboardSupport.BuildWindowsVersionDistributionAsync(
            assetAnalyticsQuery,
            cancellationToken);

        var licenseUtilization = await HomeDashboardSupport.BuildLicenseUtilizationAsync(
            licenseAnalyticsQuery,
            cancellationToken);
        viewModel.LicenseUtilization = licenseUtilization.Items;
        viewModel.TotalSoftwareLicenses = licenseUtilization.TotalLicenses;
        viewModel.AssignedSoftwareLicenses = licenseUtilization.AssignedLicenses;
        viewModel.SoftwareLicenseBreakdown = new Dictionary<string, int>
        {
            { "Assigned", licenseUtilization.AssignedLicenses },
            { "Available", Math.Max(licenseUtilization.TotalLicenses - licenseUtilization.AssignedLicenses, 0) }
        };

        var today = DateTime.UtcNow.Date;
        viewModel.ExpiredWarrantyCount = await assetAnalyticsQuery
            .CountAsync(asset =>
                !asset.IsDeleted &&
                !asset.NoExtendWarranty &&
                asset.WarrantyExpiry.HasValue &&
                asset.WarrantyExpiry.Value < today,
                cancellationToken);
        viewModel.ExpiringWithin30Days = await assetAnalyticsQuery
            .CountAsync(asset =>
                !asset.IsDeleted &&
                !asset.NoExtendWarranty &&
                asset.WarrantyExpiry.HasValue &&
                asset.WarrantyExpiry.Value >= today &&
                asset.WarrantyExpiry.Value <= today.AddDays(30),
                cancellationToken);
        viewModel.ExpiringWithin90Days = await assetAnalyticsQuery
            .CountAsync(asset =>
                !asset.IsDeleted &&
                !asset.NoExtendWarranty &&
                asset.WarrantyExpiry.HasValue &&
                asset.WarrantyExpiry.Value > today.AddDays(30) &&
                asset.WarrantyExpiry.Value <= today.AddDays(90),
                cancellationToken);

        viewModel.TotalAssets = await assetAnalyticsQuery
            .CountAsync(cancellationToken);
        viewModel.TotalLicenses = await licenseAnalyticsQuery
            .Select(license => license.StandardSoftwareId)
            .Distinct()
            .CountAsync(cancellationToken);
        viewModel.TotalActiveUsers = companyDisplayFilters.Count == 0
            ? await _context.Users
                .AsNoTracking()
                .CountAsync(userEntry => userEntry.IsActive, cancellationToken)
            : (await _context.Users
                .AsNoTracking()
                .Include(userEntry => userEntry.UserCompanies)
                .Where(userEntry => userEntry.IsActive)
                .ToListAsync(cancellationToken))
                .Count(userEntry => CompanyScopeHelper.HasAnyAllowedCompany(
                    companyDisplayFilters,
                    userEntry.UserCompanies.Select(company => company.CompanyName)));

        await HomeDashboardSupport.PopulateAuditMetricsAsync(
            _context,
            viewModel,
            period.StatsStartDate,
            period.StatsEndDate,
            cancellationToken);

        viewModel.PendingITApprovalCount =
            await onboardingAnalyticsQuery
                .CountAsync(onboarding => onboarding.Status == RequestStatus.PendingITApproval, cancellationToken)
            + await offboardingAnalyticsQuery
                .CountAsync(offboarding => offboarding.Status == RequestStatus.PendingITApproval, cancellationToken);
        viewModel.RejectedThisMonth =
            await onboardingAnalyticsQuery
                .CountAsync(onboarding =>
                    onboarding.Status == RequestStatus.Rejected &&
                    onboarding.UpdatedAt >= period.StatsStartDate &&
                    onboarding.UpdatedAt <= period.StatsEndDate,
                    cancellationToken)
            + await offboardingAnalyticsQuery
                .CountAsync(offboarding =>
                    offboarding.Status == RequestStatus.Rejected &&
                    offboarding.UpdatedAt >= period.StatsStartDate &&
                    offboarding.UpdatedAt <= period.StatsEndDate,
                    cancellationToken);
        viewModel.MaintenanceCountYear = await maintenanceAnalyticsQuery
            .CountAsync(log => log.ServiceDate >= period.YearStart && log.ServiceDate <= period.YearEnd, cancellationToken);

        var onboardingTargetDays = await _context.SlaSettings
            .AsNoTracking()
            .Where(setting => setting.RequestType == "Onboarding")
            .Select(setting => (int?)setting.TargetDays)
            .FirstOrDefaultAsync(cancellationToken) ?? 3;
        var companies = await _context.Companies
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var completedSlaSamples = await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= period.YearStart &&
                onboarding.RequestDate <= period.YearEnd &&
                onboarding.Status == RequestStatus.Completed)
            .Select(onboarding => new
            {
                onboarding.Branch,
                onboarding.RequestDate,
                onboarding.RequesterSignedAt,
                onboarding.ITAdminSignedAt,
                onboarding.EmployeeSignedAt,
                onboarding.CompletedAt
            })
            .ToListAsync(cancellationToken);

        if (completedSlaSamples.Count > 0)
        {
            var hrDurations = completedSlaSamples
                .Where(sample => sample.RequesterSignedAt.HasValue)
                .Select(sample =>
                {
                    var hoursPerDay = CompanyBusinessHoursHelper.Resolve(companies, sample.Branch).HoursPerDay;
                    if (hoursPerDay <= 0)
                    {
                        return 0d;
                    }

                    return CompanyBusinessHoursHelper.CalculateBusinessHoursSpent(
                        companies,
                        sample.Branch,
                        sample.RequestDate,
                        sample.RequesterSignedAt!.Value) / hoursPerDay;
                })
                .ToList();
            var itDurations = completedSlaSamples
                .Where(sample => sample.RequesterSignedAt.HasValue && sample.ITAdminSignedAt.HasValue)
                .Select(sample =>
                {
                    var hoursPerDay = CompanyBusinessHoursHelper.Resolve(companies, sample.Branch).HoursPerDay;
                    if (hoursPerDay <= 0)
                    {
                        return 0d;
                    }

                    return CompanyBusinessHoursHelper.CalculateBusinessHoursSpent(
                        companies,
                        sample.Branch,
                        sample.RequesterSignedAt!.Value,
                        sample.ITAdminSignedAt!.Value) / hoursPerDay;
                })
                .ToList();
            var employeeDurations = completedSlaSamples
                .Where(sample => sample.ITAdminSignedAt.HasValue && (sample.EmployeeSignedAt.HasValue || sample.CompletedAt.HasValue))
                .Select(sample =>
                {
                    var hoursPerDay = CompanyBusinessHoursHelper.Resolve(companies, sample.Branch).HoursPerDay;
                    if (hoursPerDay <= 0)
                    {
                        return 0d;
                    }

                    return CompanyBusinessHoursHelper.CalculateBusinessHoursSpent(
                        companies,
                        sample.Branch,
                        sample.ITAdminSignedAt!.Value,
                        sample.EmployeeSignedAt ?? sample.CompletedAt!.Value) / hoursPerDay;
                })
                .ToList();

            viewModel.AvgHrPhaseDays = hrDurations.Count > 0 ? Math.Round(hrDurations.Average(), 1) : 0;
            viewModel.AvgItPhaseDays = itDurations.Count > 0 ? Math.Round(itDurations.Average(), 1) : 0;
            viewModel.AvgEmployeePhaseDays = employeeDurations.Count > 0 ? Math.Round(employeeDurations.Average(), 1) : 0;

            var onTrack = 0;
            var delayed = 0;

            foreach (var sample in completedSlaSamples)
            {
                var finalDate = sample.EmployeeSignedAt ?? sample.CompletedAt;
                if (!finalDate.HasValue)
                {
                    continue;
                }

                var deadline = CompanyBusinessHoursHelper.CalculateSlaDeadline(
                    companies,
                    sample.Branch,
                    sample.RequesterSignedAt ?? sample.RequestDate,
                    onboardingTargetDays);
                if (finalDate.Value <= deadline)
                {
                    onTrack++;
                }
                else
                {
                    delayed++;
                }
            }

            viewModel.SlaComplianceDistribution = new Dictionary<string, int>
            {
                { "On Track", onTrack },
                { "Delayed", delayed }
            };
        }
        else
        {
            viewModel.SlaComplianceDistribution = new Dictionary<string, int>
            {
                { "On Track", 0 },
                { "Delayed", 0 }
            };
        }

        var previousYearStart = new DateTime(period.SelectedYear - 1, 1, 1);
        var previousYearEnd = new DateTime(period.SelectedYear - 1, 12, 31, 23, 59, 59);

        viewModel.PrevYearOnboardingCount = await onboardingAnalyticsQuery
            .CountAsync(onboarding =>
                onboarding.RequestDate >= previousYearStart &&
                onboarding.RequestDate <= previousYearEnd,
                cancellationToken);
        viewModel.PrevYearOffboardingCount = await offboardingAnalyticsQuery
            .CountAsync(offboarding =>
                offboarding.RequestDate >= previousYearStart &&
                offboarding.RequestDate <= previousYearEnd,
                cancellationToken);
        viewModel.PrevYearTotalAssets = await assetAnalyticsQuery
            .CountAsync(asset =>
                asset.PurchaseDate != null &&
                asset.PurchaseDate <= previousYearEnd,
                cancellationToken);

        var previousCompletedOnboarding = await onboardingAnalyticsQuery
            .Where(onboarding =>
                onboarding.RequestDate >= previousYearStart &&
                onboarding.RequestDate <= previousYearEnd &&
                onboarding.CompletedAt.HasValue)
            .Select(onboarding => new
            {
                onboarding.RequestDate,
                onboarding.CompletedAt
            })
            .ToListAsync(cancellationToken);

        if (previousCompletedOnboarding.Count > 0)
        {
            viewModel.PrevYearAvgOnboardingDays = Math.Round(
                previousCompletedOnboarding.Average(onboarding => (onboarding.CompletedAt!.Value - onboarding.RequestDate).TotalDays),
                1);
        }

        if (isSuperAdmin)
        {
            await PopulateSuperAdminAnalyticsAsync(viewModel, cancellationToken);
        }

        return ProcessResult<HomePageResult>.Success(new HomePageResult(
            viewModel,
            period.IsCurrentYear,
            period.StatsPeriodLabel,
            companyOptions,
            selectedCompany?.Id,
            selectedCompany?.DisplayName));
    }

    private async Task PopulateSuperAdminAnalyticsAsync(DashboardViewModel viewModel, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var since24h = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);
        var settings = await _context.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);

        viewModel.SuperAdminActiveCompanyCount = await _context.Companies.CountAsync(company => company.IsActive, cancellationToken);
        viewModel.SuperAdminInactiveCompanyCount = await _context.Companies.CountAsync(company => !company.IsActive, cancellationToken);
        viewModel.SuperAdminActiveUserCount = await _context.Users.CountAsync(user => user.IsActive, cancellationToken);
        viewModel.SuperAdminInactiveUserCount = await _context.Users.CountAsync(user => !user.IsActive, cancellationToken);
        viewModel.SuperAdminLockedUserCount = (await _context.Users
            .AsNoTracking()
            .Where(user => user.LockoutEnd != null)
            .Select(user => user.LockoutEnd)
            .ToListAsync(cancellationToken))
            .Count(lockoutEnd => lockoutEnd > now);
        viewModel.SuperAdminFailedLoginCount24h = await _context.AuditLogs.CountAsync(log => log.CreatedAt >= since24h && log.Action.Contains("Login") && !log.IsSuccess, cancellationToken);
        viewModel.SuperAdminFailedOtpCount24h = await _context.OtpRecords.CountAsync(otp => otp.CreatedAt >= since24h && otp.FailedAttempts > 0, cancellationToken);
        viewModel.SuperAdminAuditFailureCount7d = await _context.AuditLogs.CountAsync(log => log.CreatedAt >= since7d && !log.IsSuccess, cancellationToken);
        viewModel.SuperAdminHighRiskAuditCount7d = await _context.AuditLogs.CountAsync(log =>
            log.CreatedAt >= since7d &&
            (!log.IsSuccess ||
             log.Action.Contains("Delete") ||
             log.Action.Contains("Quota") ||
             log.Action.Contains("SuperAdmin") ||
             log.Action.Contains("Maintenance") ||
             log.Action.Contains("Backup")),
            cancellationToken);
        viewModel.SuperAdminMaintenanceModeEnabled = ReadBool(settings, "ControlPlane.Maintenance.Enabled");
        viewModel.SuperAdminMaintenanceModeReason = settings.GetValueOrDefault("ControlPlane.Maintenance.Reason");

        var quotaRows = await BuildSuperAdminQuotaRowsAsync(settings, cancellationToken);
        viewModel.SuperAdminQuotaUsage = quotaRows.ToDictionary(row => row.Label, row => row.Usage);
        viewModel.SuperAdminQuotaLimits = quotaRows.ToDictionary(row => row.Label, row => row.Limit);
        viewModel.SuperAdminQuotaFrozenCount = quotaRows.Count(row => row.ReadOnly);
        viewModel.SuperAdminQuotaPressureCount = quotaRows.Count(row => row.Limit > 0 && row.Usage >= Math.Ceiling(row.Limit * 0.9m));

        viewModel.SuperAdminControlPlaneActions = await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.CreatedAt >= since7d && (log.EntityName == "ControlPlane" || log.EntityName == "License"))
            .GroupBy(log => log.Action)
            .Select(group => new { Action = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .Take(8)
            .ToDictionaryAsync(item => item.Action, item => item.Count, cancellationToken);
        viewModel.SuperAdminRecentControlEvents = await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.CreatedAt >= since7d && (log.EntityName == "ControlPlane" || log.EntityName == "License" || log.Action.Contains("SuperAdmin")))
            .OrderByDescending(log => log.CreatedAt)
            .Take(10)
            .Select(log => new DashboardAuditLogItem
            {
                Id = log.Id,
                UserId = log.UserId,
                Timestamp = log.Timestamp,
                CreatedAt = log.CreatedAt,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                PerformedBy = log.PerformedBy,
                Details = log.Details,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<SuperAdminQuotaRow>> BuildSuperAdminQuotaRowsAsync(IReadOnlyDictionary<string, string> settings, CancellationToken cancellationToken)
    {
        var rows = new List<SuperAdminQuotaRow>
        {
            new("Assets", await _context.ITAssets.CountAsync(cancellationToken), ReadInt(settings, "ControlPlane.Quota.assets.Limit", 100), ReadBool(settings, "ControlPlane.Quota.assets.ReadOnly")),
            new("Software", await _context.StandardSoftware.CountAsync(cancellationToken), ReadInt(settings, "ControlPlane.Quota.software.Limit", 10), ReadBool(settings, "ControlPlane.Quota.software.ReadOnly")),
            new("Software Versions", await _context.SoftwareLicenses.CountAsync(cancellationToken), ReadInt(settings, "ControlPlane.Quota.softwareVersions.Limit", 20), ReadBool(settings, "ControlPlane.Quota.softwareVersions.ReadOnly")),
            new("Companies", await _context.Companies.CountAsync(company => company.IsActive, cancellationToken), ReadInt(settings, "ControlPlane.Quota.companies.Limit", 3), ReadBool(settings, "ControlPlane.Quota.companies.ReadOnly")),
            new("Users", await SystemUserQuotaSupport.CountableSystemUsers(_context.Users.AsNoTracking()).CountAsync(cancellationToken), ReadInt(settings, "ControlPlane.Quota.users.Limit", 10), ReadBool(settings, "ControlPlane.Quota.users.ReadOnly")),
            new("Departments", await _context.Departments.CountAsync(department => department.IsActive, cancellationToken), ReadInt(settings, "ControlPlane.Quota.departments.Limit", 10), ReadBool(settings, "ControlPlane.Quota.departments.ReadOnly")),
            new("Email Groups", await _context.EmailGroups.CountAsync(group => group.IsActive, cancellationToken), ReadInt(settings, "ControlPlane.Quota.emailGroups.Limit", 25), ReadBool(settings, "ControlPlane.Quota.emailGroups.ReadOnly")),
            new("Shared Mailboxes", await _context.SharedMailboxes.CountAsync(mailbox => mailbox.IsActive, cancellationToken), ReadInt(settings, "ControlPlane.Quota.sharedMailboxes.Limit", 25), ReadBool(settings, "ControlPlane.Quota.sharedMailboxes.ReadOnly")),
            new("Server Folders", await _context.ServerFolders.CountAsync(folder => folder.IsActive, cancellationToken), ReadInt(settings, "ControlPlane.Quota.serverFolders.Limit", 25), ReadBool(settings, "ControlPlane.Quota.serverFolders.ReadOnly"))
        };

        return rows;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
        => settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;

    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;

    private sealed record SuperAdminQuotaRow(string Label, int Usage, int Limit, bool ReadOnly);
}

/// <summary>
/// Handles the GetHomeDiagAnalyticsQuery query workflow.
/// </summary>
public sealed class GetHomeDiagAnalyticsQueryHandler : IRequestHandler<GetHomeDiagAnalyticsQuery, HomeDiagAnalyticsResult>
{
    private readonly ApplicationDbContext _context;

    public GetHomeDiagAnalyticsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<HomeDiagAnalyticsResult> Handle(GetHomeDiagAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var assetTypeDistribution = (await _context.ITAssets
            .AsNoTracking()
            .GroupBy(asset => asset.AssetType)
            .Select(group => new
            {
                Type = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Type.ToString(), item => item.Count);

        var assetStatusDistribution = (await _context.ITAssets
            .AsNoTracking()
            .GroupBy(asset => asset.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(item => item.Status.ToString(), item => item.Count);

        var licenseUtilization = await HomeDashboardSupport.BuildLicenseUtilizationAsync(_context, cancellationToken);
        var softwareDistribution = new Dictionary<string, int>
        {
            { "Assigned", licenseUtilization.AssignedLicenses },
            { "Available", Math.Max(licenseUtilization.TotalLicenses - licenseUtilization.AssignedLicenses, 0) }
        };

        return new HomeDiagAnalyticsResult(
            assetTypeDistribution,
            assetStatusDistribution,
            softwareDistribution);
    }
}

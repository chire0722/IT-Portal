// Project comment: HomeContracts.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Home;

/// <summary>
/// Carries data needed to execute the GetHomeDashboard query.
/// </summary>
public sealed record GetHomeDashboardQuery(int? Year) : IRequest<HomePageResult>;

/// <summary>
/// Carries data needed to execute the GetHomeAnalytics query.
/// </summary>
public sealed record GetHomeAnalyticsQuery(int? Year, int? CompanyId) : IRequest<ProcessResult<HomePageResult>>;

/// <summary>
/// Carries data needed to execute the GetHomeDiagAnalytics query.
/// </summary>
public sealed record GetHomeDiagAnalyticsQuery() : IRequest<HomeDiagAnalyticsResult>;

/// <summary>
/// Represents the result returned by the HomePage workflow.
/// </summary>
public sealed record HomePageResult(
    DashboardViewModel ViewModel,
    bool IsCurrentYear,
    string StatsPeriodLabel,
    IReadOnlyList<PageCompanyOption> CompanyOptions,
    int? SelectedCompanyId,
    string? SelectedCompanyDisplayName);

/// <summary>
/// Represents the result returned by the HomeDiagAnalytics workflow.
/// </summary>
public sealed record HomeDiagAnalyticsResult(
    IReadOnlyDictionary<string, int> AssetType,
    IReadOnlyDictionary<string, int> AssetStatus,
    IReadOnlyDictionary<string, int> SoftwareLicense);

/// <summary>
/// Defines the DashboardPeriodContext record used by the IT Form application.
/// </summary>
internal sealed record DashboardPeriodContext(
    int SelectedYear,
    bool IsCurrentYear,
    DateTime StatsStartDate,
    DateTime StatsEndDate,
    DateTime YearStart,
    DateTime YearEnd,
    IReadOnlyList<int> AvailableYears,
    string StatsPeriodLabel);

/// <summary>
/// Represents the result returned by the MonthlyTrend workflow.
/// </summary>
internal sealed record MonthlyTrendResult(
    List<string> Labels,
    List<int> Onboarding,
    List<int> Offboarding);

/// <summary>
/// Represents the result returned by the LicenseUtilization workflow.
/// </summary>
internal sealed record LicenseUtilizationResult(
    List<SoftwareUtilizationViewModel> Items,
    int TotalLicenses,
    int AssignedLicenses);

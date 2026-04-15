using IT_Form.Application.Common.Models;
using IT_Form.Models;
using MediatR;

namespace IT_Form.Application.Features.Home;

public sealed record GetHomeDashboardQuery(int? Year) : IRequest<HomePageResult>;

public sealed record GetHomeAnalyticsQuery(int? Year, int? CompanyId) : IRequest<ProcessResult<HomePageResult>>;

public sealed record GetHomeDiagAnalyticsQuery() : IRequest<HomeDiagAnalyticsResult>;

public sealed record HomePageResult(
    DashboardViewModel ViewModel,
    bool IsCurrentYear,
    string StatsPeriodLabel,
    IReadOnlyList<PageCompanyOption> CompanyOptions,
    int? SelectedCompanyId,
    string? SelectedCompanyDisplayName);

public sealed record HomeDiagAnalyticsResult(
    IReadOnlyDictionary<string, int> AssetType,
    IReadOnlyDictionary<string, int> AssetStatus,
    IReadOnlyDictionary<string, int> SoftwareLicense);

internal sealed record DashboardPeriodContext(
    int SelectedYear,
    bool IsCurrentYear,
    DateTime StatsStartDate,
    DateTime StatsEndDate,
    DateTime YearStart,
    DateTime YearEnd,
    IReadOnlyList<int> AvailableYears,
    string StatsPeriodLabel);

internal sealed record MonthlyTrendResult(
    List<string> Labels,
    List<int> Onboarding,
    List<int> Offboarding);

internal sealed record LicenseUtilizationResult(
    List<SoftwareUtilizationViewModel> Items,
    int TotalLicenses,
    int AssignedLicenses);

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT_Form.Application.Common.Support;

internal static class PageQueryLookupSupport
{
    public static Task<List<PageCompanyOption>> LoadActiveCompaniesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
        => context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .ProjectToType<PageCompanyOption>()
            .ToListAsync(cancellationToken);

    public static async Task<List<PageCompanyOption>> LoadActiveCompaniesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
        => (await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(context, user, cancellationToken))
            .Select(company => new PageCompanyOption
            {
                DisplayName = company
            })
            .ToList();

    public static async Task<List<PageCurrentEmployeeOption>> LoadCurrentEmployeesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var currentEmployees = await context.OnboardingRequests
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.Completed || request.Status == RequestStatus.Approved)
            .OrderBy(request => request.FullName)
            .ToListAsync(cancellationToken);

        return currentEmployees
            .Select(request => new PageCurrentEmployeeOption
            {
                Id = request.Id,
                Label = FormatCurrentEmployeeLabel(request.FullName, request.EmployeeID)
            })
            .ToList();
    }

    public static async Task<List<PageCurrentEmployeeOption>> LoadCurrentEmployeesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
        var query = CompanyScopeHelper.ApplyOnboardingCompanyScope(
            context.OnboardingRequests
            .AsNoTracking()
            .Where(request => request.Status == RequestStatus.Completed || request.Status == RequestStatus.Approved),
            scopedCompanies);

        var currentEmployees = await query
            .OrderBy(request => request.FullName)
            .ToListAsync(cancellationToken);

        return currentEmployees
            .Select(request => new PageCurrentEmployeeOption
            {
                Id = request.Id,
                Label = FormatCurrentEmployeeLabel(request.FullName, request.EmployeeID)
            })
            .ToList();
    }

    public static Task<string?> LoadCurrentEmployeeDisplayNameAsync(
        ApplicationDbContext context,
        int? onboardingRequestId,
        CancellationToken cancellationToken)
    {
        if (!onboardingRequestId.HasValue)
        {
            return Task.FromResult<string?>(null);
        }

        return LoadCurrentEmployeeDisplayNameCoreAsync(context, onboardingRequestId.Value, cancellationToken);
    }

    private static async Task<string?> LoadCurrentEmployeeDisplayNameCoreAsync(
        ApplicationDbContext context,
        int onboardingRequestId,
        CancellationToken cancellationToken)
    {
        var currentEmployee = await context.OnboardingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == onboardingRequestId, cancellationToken);

        return currentEmployee == null
            ? null
            : FormatCurrentEmployeeLabel(currentEmployee.FullName, currentEmployee.EmployeeID);
    }

    private static string FormatCurrentEmployeeLabel(string? fullName, string? employeeId)
        => (fullName ?? "Unknown") + " (" + (employeeId ?? "-") + ")";
}

// Project comment: PageQueryLookupSupport.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IT_Form.Application.Common.Support;

/// <summary>
/// Defines the PageQueryLookupSupport class used by the IT Form application.
/// </summary>
internal static class PageQueryLookupSupport
{
    /// <summary>
    /// Runs the LoadActiveCompaniesAsync operation.
    /// </summary>
    public static Task<List<PageCompanyOption>> LoadActiveCompaniesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
        => context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .ProjectToType<PageCompanyOption>()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Runs the LoadActiveCompaniesAsync operation.
    /// </summary>
    public static async Task<List<PageCompanyOption>> LoadActiveCompaniesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(user);
        var companies = await context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .Select(company => new PageCompanyOption
            {
                Id = company.Id,
                DisplayName = string.IsNullOrWhiteSpace(company.Lot)
                    ? company.Name
                    : company.Name + " (" + company.Lot + ")"
            })
            .ToListAsync(cancellationToken);

        if (scopedCompanies.Count == 0)
        {
            return companies;
        }

        return companies
            .Where(company => CompanyScopeHelper.CanAccessCompany(scopedCompanies, company.DisplayName))
            .ToList();
    }

    /// <summary>
    /// Runs the LoadCurrentEmployeesAsync operation.
    /// </summary>
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

    /// <summary>
    /// Runs the LoadCurrentEmployeesAsync operation.
    /// </summary>
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

    /// <summary>
    /// Runs the LoadCurrentEmployeeDisplayNameAsync operation.
    /// </summary>
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

    /// <summary>
    /// Runs the LoadCurrentEmployeeDisplayNameCoreAsync operation.
    /// </summary>
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

    /// <summary>
    /// Runs the FormatCurrentEmployeeLabel operation.
    /// </summary>
    private static string FormatCurrentEmployeeLabel(string? fullName, string? employeeId)
        => (fullName ?? "Unknown") + " (" + (employeeId ?? "-") + ")";
}

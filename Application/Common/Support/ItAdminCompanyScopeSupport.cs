// Project comment: ItAdminCompanyScopeSupport.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Security;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Common.Support;

/// <summary>
/// Defines the ItAdminCompanyScopeSupport class used by the IT Form application.
/// </summary>
internal static class ItAdminCompanyScopeSupport
{
    /// <summary>
    /// Runs the HasGlobalAccess operation.
    /// </summary>
    public static bool HasGlobalAccess(ClaimsPrincipal? user)
        => PortalTitles.IsSuperAdmin(user) ||
           (PortalTitles.IsPrivilegedIt(user) &&
            user?.Claims.Any(claim =>
                claim.Type == "UserCompany" &&
                !string.IsNullOrWhiteSpace(claim.Value)) != true);

    /// <summary>
    /// Gets the data required by the GetScopedCompanies operation.
    /// </summary>
    public static List<string> GetScopedCompanies(ClaimsPrincipal? user)
        => HasGlobalAccess(user)
            ? []
            : CompanyScopeHelper.GetScopedCompanies(user);

    /// <summary>
    /// Runs the LoadAccessibleCompanyIdsAsync operation.
    /// </summary>
    public static async Task<HashSet<int>?> LoadAccessibleCompanyIdsAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken,
        bool activeOnly = false)
    {
        if (HasGlobalAccess(user))
        {
            return null;
        }

        return await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(
            context,
            user,
            cancellationToken,
            activeOnly);
    }

    /// <summary>
    /// Runs the LoadAccessibleCompanyDisplayNamesAsync operation.
    /// </summary>
    public static async Task<List<string>> LoadAccessibleCompanyDisplayNamesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        if (!HasGlobalAccess(user))
        {
            return await CompanyScopeHelper.LoadAccessibleCompanyDisplayNamesAsync(context, user, cancellationToken);
        }

        return await context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .Select(company => string.IsNullOrWhiteSpace(company.Lot)
                ? company.Name
                : company.Name + " (" + company.Lot + ")")
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Runs the LoadCompanyOptionsAsync operation.
    /// </summary>
    public static Task<List<PageCompanyOption>> LoadCompanyOptionsAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
        => HasGlobalAccess(user)
            ? PageQueryLookupSupport.LoadActiveCompaniesAsync(context, cancellationToken)
            : PageQueryLookupSupport.LoadActiveCompaniesAsync(context, user, cancellationToken);

    /// <summary>
    /// Runs the LoadCurrentEmployeesAsync operation.
    /// </summary>
    public static Task<List<PageCurrentEmployeeOption>> LoadCurrentEmployeesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
        => HasGlobalAccess(user)
            ? PageQueryLookupSupport.LoadCurrentEmployeesAsync(context, cancellationToken)
            : PageQueryLookupSupport.LoadCurrentEmployeesAsync(context, user, cancellationToken);

    /// <summary>
    /// Runs the CanAccessCompany operation.
    /// </summary>
    public static bool CanAccessCompany(ClaimsPrincipal? user, string? company)
        => HasGlobalAccess(user) || CompanyScopeHelper.CanAccessCompany(user, company);

    /// <summary>
    /// Runs the AreRequestedCompanyIdsAssignableAsync operation.
    /// </summary>
    public static async Task<bool> AreRequestedCompanyIdsAssignableAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        IEnumerable<int> requestedCompanyIds,
        CancellationToken cancellationToken)
    {
        var requestedIds = requestedCompanyIds
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return GetScopedCompanies(user).Count == 0;
        }

        if (!HasGlobalAccess(user))
        {
            return await CompanyScopeHelper.AreRequestedCompanyIdsAssignableAsync(
                context,
                user,
                requestedIds,
                cancellationToken);
        }

        var activeCompanyIds = await context.Companies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .Select(company => company.Id)
            .ToHashSetAsync(cancellationToken);

        return requestedIds.All(activeCompanyIds.Contains);
    }
}

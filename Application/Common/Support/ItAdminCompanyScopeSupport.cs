using System.Security.Claims;
using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Common.Support;

internal static class ItAdminCompanyScopeSupport
{
    public static bool HasGlobalAccess(ClaimsPrincipal? user)
        => user?.HasClaim("UserTitle", "IT") == true;

    public static List<string> GetScopedCompanies(ClaimsPrincipal? user)
        => HasGlobalAccess(user)
            ? []
            : CompanyScopeHelper.GetScopedCompanies(user);

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

    public static Task<List<PageCompanyOption>> LoadCompanyOptionsAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
        => HasGlobalAccess(user)
            ? PageQueryLookupSupport.LoadActiveCompaniesAsync(context, cancellationToken)
            : PageQueryLookupSupport.LoadActiveCompaniesAsync(context, user, cancellationToken);

    public static Task<List<PageCurrentEmployeeOption>> LoadCurrentEmployeesAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
        => HasGlobalAccess(user)
            ? PageQueryLookupSupport.LoadCurrentEmployeesAsync(context, cancellationToken)
            : PageQueryLookupSupport.LoadCurrentEmployeesAsync(context, user, cancellationToken);

    public static bool CanAccessCompany(ClaimsPrincipal? user, string? company)
        => HasGlobalAccess(user) || CompanyScopeHelper.CanAccessCompany(user, company);

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
            return false;
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

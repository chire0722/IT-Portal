using System.Security.Claims;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware;

/// <summary>
/// Enforces company ownership of software licenses before exposing or mutating license secrets.
/// </summary>
internal static class StandardSoftwareLicenseScopeSupport
{
    public static bool CanAccessEmployee(ClaimsPrincipal? user, string? company)
        => CompanyScopeHelper.CanAccessCompany(user, company);

    public static async Task<bool> CanAccessAsync(
        ApplicationDbContext context,
        ClaimsPrincipal? user,
        int softwareLicenseId,
        CancellationToken cancellationToken)
    {
        if (ItAdminCompanyScopeSupport.HasGlobalAccess(user))
        {
            return true;
        }

        var licenseCompanyIds = await context.SoftwareLicenseCompanies
            .AsNoTracking()
            .Where(link => link.SoftwareLicenseId == softwareLicenseId)
            .Select(link => link.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Unscoped legacy licenses are visible only to explicitly global IT administrators.
        if (licenseCompanyIds.Count == 0)
        {
            return false;
        }

        var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(
            context,
            user,
            cancellationToken);

        return accessibleCompanyIds != null &&
               licenseCompanyIds.All(accessibleCompanyIds.Contains);
    }
}

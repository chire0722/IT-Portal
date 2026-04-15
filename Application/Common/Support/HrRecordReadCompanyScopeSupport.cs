using System.Security.Claims;
using IT_Form.Helpers;

namespace IT_Form.Application.Common.Support;

internal static class HrRecordReadCompanyScopeSupport
{
    public static bool HasGlobalReadAccess(ClaimsPrincipal? user)
        => user?.HasClaim("UserTitle", "HR") == true;

    public static List<string> GetScopedCompanies(ClaimsPrincipal? user)
        => HasGlobalReadAccess(user)
            ? []
            : CompanyScopeHelper.GetScopedCompanies(user);

    public static bool CanAccessCompany(ClaimsPrincipal? user, string? company)
        => HasGlobalReadAccess(user) || CompanyScopeHelper.CanAccessCompany(user, company);
}

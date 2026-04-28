// Project comment: HrRecordReadCompanyScopeSupport.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Helpers;

namespace IT_Form.Application.Common.Support;

/// <summary>
/// Defines the HrRecordReadCompanyScopeSupport class used by the IT Form application.
/// </summary>
internal static class HrRecordReadCompanyScopeSupport
{
    /// <summary>
    /// Runs the HasGlobalReadAccess operation.
    /// </summary>
    public static bool HasGlobalReadAccess(ClaimsPrincipal? user)
        => user?.HasClaim("UserTitle", "HR") == true;

    /// <summary>
    /// Gets the data required by the GetScopedCompanies operation.
    /// </summary>
    public static List<string> GetScopedCompanies(ClaimsPrincipal? user)
        => HasGlobalReadAccess(user)
            ? []
            : CompanyScopeHelper.GetScopedCompanies(user);

    /// <summary>
    /// Runs the CanAccessCompany operation.
    /// </summary>
    public static bool CanAccessCompany(ClaimsPrincipal? user, string? company)
        => HasGlobalReadAccess(user) || CompanyScopeHelper.CanAccessCompany(user, company);
}

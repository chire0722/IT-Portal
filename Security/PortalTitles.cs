// Project comment: PortalTitles.cs defines protected title constants for authorization checks.

namespace IT_Form.Security;

public static class PortalTitles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string IT = "IT";
    public const string HR = "HR";
    public const string HOD = "HOD";

    public static bool IsPrivilegedIt(System.Security.Claims.ClaimsPrincipal? user)
        => user?.HasClaim("UserTitle", SuperAdmin) == true || user?.HasClaim("UserTitle", IT) == true;

    public static bool IsSuperAdmin(System.Security.Claims.ClaimsPrincipal? user)
        => user?.HasClaim("UserTitle", SuperAdmin) == true;

    public static bool HasGlobalCompanyAccess(System.Security.Claims.ClaimsPrincipal? user)
        => IsPrivilegedIt(user);

    public static bool CanManageSuperAdmin(System.Security.Claims.ClaimsPrincipal? user)
        => IsSuperAdmin(user);
}

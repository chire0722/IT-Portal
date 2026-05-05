// Project comment: UserManagementVisibility.cs centralizes protected user visibility rules.

using System.Security.Claims;
using IT_Form.Models;
using IT_Form.Security;

namespace IT_Form.Application.Features.UserManagement;

internal static class UserManagementVisibility
{
    public static IQueryable<ApplicationUser> ApplySuperAdminVisibility(
        IQueryable<ApplicationUser> users,
        ClaimsPrincipal? viewer)
        => CanSeeSuperAdminUsers(viewer)
            ? users
            : users.Where(user => !user.UserTitles.Any(title => title.Title == PortalTitles.SuperAdmin));

    public static bool CanSeeSuperAdminUsers(ClaimsPrincipal? viewer)
        => PortalTitles.IsSuperAdmin(viewer);

    public static bool IsSuperAdminUser(ApplicationUser user)
        => user.UserTitles.Any(title => title.Title == PortalTitles.SuperAdmin);
}

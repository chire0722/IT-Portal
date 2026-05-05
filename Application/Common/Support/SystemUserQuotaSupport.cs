// Project comment: SystemUserQuotaSupport.cs centralizes quota-countable system user filters.

using IT_Form.Models;
using IT_Form.Security;

namespace IT_Form.Application.Common.Support;

internal static class SystemUserQuotaSupport
{
    public static IQueryable<ApplicationUser> CountableSystemUsers(IQueryable<ApplicationUser> users)
        => users.Where(user =>
            user.IsActive &&
            !user.UserTitles.Any(title => title.Title == PortalTitles.SuperAdmin));
}

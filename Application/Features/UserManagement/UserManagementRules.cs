// Project comment: UserManagementRules.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Security;

namespace IT_Form.Application.Features.UserManagement
{
    /// <summary>
    /// Defines the UserManagementRules class used by the IT Form application.
    /// </summary>
    internal static class UserManagementRules
    {
        /// <summary>
        /// Runs the AssignTitlesAsync operation.
        /// </summary>
        public static async Task AssignTitlesAsync(ApplicationDbContext context, string userId, bool isIt, bool isHr, bool isHod, bool isSuperAdmin = false)
        {
            if (isIt || isSuperAdmin)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = PortalTitles.IT });
            if (isHr)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = PortalTitles.HR });
            if (isHod)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = PortalTitles.HOD });
            if (isSuperAdmin)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = PortalTitles.SuperAdmin });

            await Task.CompletedTask;
        }

        /// <summary>
        /// Runs the AssignCompanies operation.
        /// </summary>
        public static void AssignCompanies(ApplicationDbContext context, string userId, List<string>? selectedCompanies)
        {
            if (selectedCompanies == null) return;
            foreach (var companyName in selectedCompanies.Distinct())
            {
                context.UserCompanies.Add(new UserCompany { UserId = userId, CompanyName = companyName });
            }
        }
    }
}

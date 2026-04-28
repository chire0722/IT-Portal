// Project comment: UserManagementRules.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;

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
        public static async Task AssignTitlesAsync(ApplicationDbContext context, string userId, bool isIt, bool isHr, bool isHod)
        {
            if (isIt)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = "IT" });
            if (isHr)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = "HR" });
            if (isHod)
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = "HOD" });

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

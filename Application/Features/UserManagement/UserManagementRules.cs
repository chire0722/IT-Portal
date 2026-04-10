using IT_Form.Data;
using IT_Form.Models;

namespace IT_Form.Application.Features.UserManagement
{
    internal static class UserManagementRules
    {
        public static bool IsAllowedDomain(string? email, SecuritySettings securitySettings)
        {
            if (securitySettings.AllowedDomains == null || !securitySettings.AllowedDomains.Any())
                return true;

            var emailParts = email?.Split('@');
            return emailParts?.Length == 2 && securitySettings.AllowedDomains.Contains(emailParts[1].ToLower());
        }

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

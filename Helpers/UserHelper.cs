// Project comment: UserHelper.cs is part of the IT Form application source code.

using System;
using IT_Form.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the UserHelper class used by the IT Form application.
    /// </summary>
    public static class UserHelper
    {
        /// <summary>
        /// Gets the data required by the GetClaimValues operation.
        /// </summary>
        public static List<string> GetClaimValues(ClaimsPrincipal? principal, string claimType)
        {
            if (principal == null || string.IsNullOrWhiteSpace(claimType))
            {
                return new List<string>();
            }

            return principal.Claims
                .Where(c => c.Type == claimType && !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// Runs the NormalizeEmailOrUserName operation.
        /// </summary>
        public static string NormalizeEmailOrUserName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalized = value.Trim().ToLowerInvariant();
            return normalized.Replace(".locla", ".local", StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the data required by the GetEmailLocalPart operation.
        /// </summary>
        public static string GetEmailLocalPart(string? value)
        {
            var normalized = NormalizeEmailOrUserName(value);
            if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

            var atIndex = normalized.IndexOf('@');
            return atIndex > 0 ? normalized[..atIndex] : normalized;
        }

        /// <summary>
        /// Runs the EmailsMatch operation.
        /// </summary>
        public static bool EmailsMatch(IEnumerable<string> candidates, string? target)
        {
            if (string.IsNullOrWhiteSpace(target)) return false;

            var normalizedTarget = NormalizeEmailOrUserName(target);
            var targetLocalPart = GetEmailLocalPart(normalizedTarget);

            foreach (var candidate in candidates.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                var normalizedCandidate = NormalizeEmailOrUserName(candidate);
                if (normalizedCandidate == normalizedTarget) return true;
                if (GetEmailLocalPart(normalizedCandidate) == targetLocalPart) return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves all possible email addresses and aliases for a given username or email,
        /// including domain variations and reporting manager aliases from the database.
        /// </summary>
        public static async Task<List<string>> GetHODEmailsAsync(ApplicationDbContext context, string currentUserName)
        {
            if (string.IsNullOrEmpty(currentUserName)) return new List<string>();

            var normalizedCurrentUserName = NormalizeEmailOrUserName(currentUserName);
            var normalizedLookup = normalizedCurrentUserName.ToUpperInvariant();
            var currentUser = await context.Users
                .AsNoTracking()
                .Where(u => u.NormalizedUserName == normalizedLookup || u.NormalizedEmail == normalizedLookup)
                .Select(u => new
                {
                    u.Email
                })
                .FirstOrDefaultAsync();
            
            var emailForBase = currentUser?.Email ?? currentUserName;
            var baseUsername = GetEmailLocalPart(emailForBase);

            // We include domain variations to handle cases where HR might use a different company domain
            // but we ONLY refer to the system user Record, not the ReportingManagers table.
            var hodEmails = new List<string> {
                currentUserName,
                emailForBase,
                baseUsername,
                $"{baseUsername}@ytcprecast.com",
                $"{baseUsername}@ytcprecast.local",
                $"{baseUsername}@yengtong.com",
                $"{baseUsername}@pembinaanyengtong.com"
            };

            // Cleanup: remove nulls/empties, convert to lower case for comparison, and remove duplicates
            return hodEmails
                .Where(e => !string.IsNullOrEmpty(e))
                .Select(NormalizeEmailOrUserName)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Runs the ResolveUserEmailAsync operation.
        /// </summary>
        public static async Task<string?> ResolveUserEmailAsync(ApplicationDbContext context, string? emailOrUsername, params string[] allowedTitles)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername)) return null;

            var normalizedValue = NormalizeEmailOrUserName(emailOrUsername);
            var localPart = GetEmailLocalPart(normalizedValue);
            var normalizedLookup = normalizedValue.ToUpperInvariant();

            var userQuery = context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Email != null);

            if (allowedTitles.Length > 0)
            {
                userQuery = userQuery.Where(u => u.UserTitles.Any(t => allowedTitles.Contains(t.Title)));
            }

            var exactEmailMatch = await userQuery
                .Where(u => u.NormalizedEmail == normalizedLookup)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(exactEmailMatch)) return exactEmailMatch.Trim();

            var usernameMatch = await userQuery
                .Where(u => u.NormalizedUserName == normalizedLookup || u.NormalizedUserName == localPart.ToUpperInvariant())
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(usernameMatch)) return usernameMatch.Trim();

            var localPartEmailMatch = await userQuery
                .Where(u => u.Email != null && u.Email.ToLower().StartsWith(localPart + "@"))
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(localPartEmailMatch)) return localPartEmailMatch.Trim();

            return normalizedValue;
        }

        /// <summary>
        /// Finds the active HOD for a given company (and optionally matching department) 
        /// from the system user records (UserTitles and UserCompanies).
        /// </summary>
        public static async Task<string?> GetLatestHODEmailByCompanyAsync(ApplicationDbContext context, string? company, string? department = null)
        {
            if (string.IsNullOrEmpty(company)) return null;

            var hodQuery = context.Users
                .Where(u => u.IsActive && u.UserTitles.Any(t => t.Title == "HOD" || t.Title == "Manager"))
                .Where(u => u.UserCompanies.Any(c => c.CompanyName == company || 
                                                     c.CompanyName.StartsWith(company + " (") || 
                                                     company.StartsWith(c.CompanyName + " (")));

            // If department is provided, try to find a manager within that department first
            if (!string.IsNullOrEmpty(department))
            {
                var deptMatch = await hodQuery
                    .Where(u => u.Department != null && u.Department.ToLower() == department.ToLower().Trim())
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(deptMatch)) return deptMatch;
            }

            // Fallback to first active HOD in that company
            return await hodQuery.Select(u => u.Email).FirstOrDefaultAsync();
        }
    }
}

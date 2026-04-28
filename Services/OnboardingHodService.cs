// Project comment: OnboardingHodService.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides OnboardingHod service behavior for the application.
    /// </summary>
    public sealed class OnboardingHodService : IOnboardingHodService
    {
        private readonly ApplicationDbContext _context;

        public OnboardingHodService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Builds the value returned by the BuildHodSelectListAsync operation.
        /// </summary>
        public async Task<SelectList> BuildHodSelectListAsync(string? company, string? selectedHodEmail = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(company))
            {
                return new SelectList(Enumerable.Empty<object>(), "Email", "Display", selectedHodEmail);
            }

            var normalizedCompany = CompanyHelper.Normalize(company).ToLower();
            var hodUsers = await _context.Users
                .Where(u => u.IsActive && u.UserTitles.Any(t => t.Title == "HOD"))
                .Where(u => u.UserCompanies.Any(c =>
                    c.CompanyName != null &&
                    (c.CompanyName.ToLower() == normalizedCompany ||
                     c.CompanyName.ToLower().StartsWith(normalizedCompany + " ("))))
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    Email = u.Email ?? string.Empty,
                    Display = $"{u.FullName} ({u.Email})"
                })
                .ToListAsync(cancellationToken);

            return new SelectList(hodUsers, "Email", "Display", selectedHodEmail);
        }

        /// <summary>
        /// Runs the IsValidHodForCompanyAsync operation.
        /// </summary>
        public async Task<bool> IsValidHodForCompanyAsync(string? company, string? hodEmail, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(hodEmail))
            {
                return false;
            }

            var normalizedCompany = CompanyHelper.Normalize(company).ToLower();
            var normalizedEmail = hodEmail.Trim().ToLower();
            return await _context.Users.AnyAsync(u =>
                u.IsActive &&
                u.Email != null &&
                u.Email.ToLower() == normalizedEmail &&
                u.UserTitles.Any(t => t.Title == "HOD") &&
                u.UserCompanies.Any(c =>
                    c.CompanyName != null &&
                    (c.CompanyName.ToLower() == normalizedCompany ||
                     c.CompanyName.ToLower().StartsWith(normalizedCompany + " ("))),
                cancellationToken);
        }
    }
}

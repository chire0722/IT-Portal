// Project comment: EmployeeController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Helpers;

namespace IT_Form.Controllers
{
    [Authorize]
    /// <summary>
    /// Handles web requests for the Employee area.
    /// </summary>
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employee Current Roster
        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(string searchString, string department, string branch, int page = 1)
        {
            // Restrict the roster to companies the current user is scoped to, preventing
            // cross-company employee directory disclosure.
            var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(User);

            // Only pull Onboarding requests that are Fully Completed (Approved)
            var employees = _context.OnboardingRequests
                .Where(o => o.Status == RequestStatus.Completed || o.Status == RequestStatus.Approved)
                .AsQueryable();

            employees = CompanyScopeHelper.ApplyOnboardingCompanyScope(employees, scopedCompanies);

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                employees = employees.Where(e => (e.FullName != null && e.FullName.ToLower().Contains(lowerSearch)) ||
                                              (e.EmployeeID != null && e.EmployeeID.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrEmpty(department))
            {
                employees = employees.Where(e => e.Department == department);
            }

            if (!string.IsNullOrEmpty(branch))
            {
                employees = employees.Where(e => e.Branch == branch);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["DeptFilter"] = department;
            ViewData["BranchFilter"] = branch;

            // Optional: get distinct departments and branches for dropdown filters.
            // These must be company-scoped too, otherwise the filter dropdowns leak
            // department/branch names belonging to other companies.
            var scopedRoster = _context.OnboardingRequests
                .Where(o => o.Status == RequestStatus.Completed || o.Status == RequestStatus.Approved)
                .AsQueryable();

            scopedRoster = CompanyScopeHelper.ApplyOnboardingCompanyScope(scopedRoster, scopedCompanies);

            ViewData["Departments"] = await scopedRoster
                .Select(o => o.Department)
                .Distinct()
                .ToListAsync();

            ViewData["Branches"] = await scopedRoster
                .Where(o => o.Branch != null)
                .Select(o => o.Branch)
                .Distinct()
                .ToListAsync();

            int pageSize = 15;
            var paginatedList = await PaginatedList<OnboardingRequest>.CreateAsync(
                employees.OrderBy(e => e.FullName), page, pageSize);

            return View(paginatedList);
        }
    }
}

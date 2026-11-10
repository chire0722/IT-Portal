// Project comment: EmployeeController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Form.Data;
using IT_Form.Application.Common.Support;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Helpers;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireHROrIT")]
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
            // Only show completed employees within the current user's company scope.
            // HR retains global read access through the shared HR-record scope policy.
            var completedRequests = _context.OnboardingRequests
                .AsNoTracking()
                .Where(o => o.Status == RequestStatus.Completed || o.Status == RequestStatus.Approved);
            var scopedCompanies = HrRecordReadCompanyScopeSupport.GetScopedCompanies(User);
            var scopedRequests = CompanyScopeHelper.ApplyCompanyScope(completedRequests, scopedCompanies, request => request.Branch);
            var employees = scopedRequests;

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

            // Optional: get distinct departments and branches for dropdown filters
            ViewData["Departments"] = await scopedRequests
                .Select(o => o.Department)
                .Distinct()
                .ToListAsync();

            ViewData["Branches"] = await scopedRequests
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

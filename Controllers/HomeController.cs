// Project comment: HomeController.cs is part of the IT Form application source code.

using System.Diagnostics;
using IT_Form.Application.Features.Home;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IT_Form.Controllers
{
    [Authorize]
    /// <summary>
    /// Handles web requests for the Home area.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ISender _sender;

        public HomeController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Runs the Index operation.
        /// </summary>
        public async Task<IActionResult> Index(int? year)
        {
            var result = await _sender.Send(new GetHomeDashboardQuery(year));
            ApplyPageMetadata(result);
            return View(result.ViewModel);
        }

        /// <summary>
        /// Runs the Analytics operation.
        /// </summary>
        public async Task<IActionResult> Analytics(int? year, int? companyId)
        {
            var result = await _sender.Send(new GetHomeAnalyticsQuery(year, companyId));
            if (result.IsForbidden)
            {
                return Forbid();
            }

            var page = result.Data!;
            ApplyPageMetadata(page);
            return View(page.ViewModel);
        }

        [Authorize(Policy = "RequireIT")]
        /// <summary>
        /// Runs the DiagAnalytics operation.
        /// </summary>
        public async Task<IActionResult> DiagAnalytics()
        {
            var result = await _sender.Send(new GetHomeDiagAnalyticsQuery());
            return Json(new
            {
                AssetType = result.AssetType,
                AssetStatus = result.AssetStatus,
                SoftwareLicense = result.SoftwareLicense
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        /// <summary>
        /// Runs the Error operation.
        /// </summary>
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        /// <summary>
        /// Runs the Privacy operation.
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Applies changes for the ApplyPageMetadata operation.
        /// </summary>
        private void ApplyPageMetadata(HomePageResult result)
        {
            ViewData["IsCurrentYear"] = result.IsCurrentYear;
            ViewData["StatsPeriodLabel"] = result.StatsPeriodLabel;
            ViewData["CompanyOptions"] = result.CompanyOptions;
            ViewData["SelectedCompanyId"] = result.SelectedCompanyId;
            ViewData["SelectedCompanyDisplayName"] = result.SelectedCompanyDisplayName;
        }
    }
}

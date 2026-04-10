using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireHROrIT")]
    [Authorize(Policy = "RequireIT")]
    public class SlaSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SlaSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SlaSettings
        public async Task<IActionResult> Index()
        {
            if (!User.HasClaim("UserTitle", "IT")) return Forbid();
            await DbInitializer.EnsureDefaultSlaSettingsAsync(_context);

            var settings = await _context.SlaSettings
                .OrderBy(s => s.RequestType == "Onboarding" ? 0 :
                              s.RequestType == "Offboarding" ? 1 :
                              2)
                .ThenBy(s => s.RequestType)
                .ToListAsync();
            return View(settings);
        }

        // POST: SlaSettings/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(List<SlaSetting> settings)
        {
            if (ModelState.IsValid)
            {
                foreach (var inputSetting in settings)
                {
                    var existing = await _context.SlaSettings.FindAsync(inputSetting.Id);
                    if (existing != null)
                    {
                        existing.TargetDays = inputSetting.TargetDays;
                        existing.NotifyEmail = inputSetting.NotifyEmail;
                        
                        existing.EscalationDaysTier1 = inputSetting.EscalationDaysTier1;
                        existing.EscalationEmailTier1 = inputSetting.EscalationEmailTier1;
                        
                        existing.EscalationDaysTier2 = inputSetting.EscalationDaysTier2;
                        existing.EscalationEmailTier2 = inputSetting.EscalationEmailTier2;

                        existing.UpdatedAt = DateTime.UtcNow;
                        _context.Update(existing);
                    }
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "SLA Targets updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update SLA settings.";
            return View("Index", settings);
        }
    }
}

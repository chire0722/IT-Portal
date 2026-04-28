// Project comment: MaintenanceLogController.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IT_Form.Data;
using IT_Form.Models;
using System.Threading.Tasks;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireIT")]
    /// <summary>
    /// Handles web requests for the MaintenanceLog area.
    /// </summary>
    public class MaintenanceLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MaintenanceLog/Create/5 (AssetId)
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create(int itAssetId)
        {
            var asset = await _context.ITAssets.FindAsync(itAssetId);
            if (asset == null) return NotFound();

            ViewBag.AssetTag = asset.AssetTag;
            return View(new MaintenanceLog { ITAssetId = itAssetId, ServiceDate = System.DateTime.Today });
        }

        // POST: MaintenanceLog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Creates data for the Create operation.
        /// </summary>
        public async Task<IActionResult> Create([Bind("ITAssetId,ServiceDate,Description,ServiceProvider,Cost,Notes")] MaintenanceLog maintenanceLog)
        {
            if (maintenanceLog == null) return View();
            if (ModelState.IsValid)
            {
                _context.Add(maintenanceLog);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "ITAsset", new { id = maintenanceLog.ITAssetId });
            }
            return View(maintenanceLog);
        }

        // GET: MaintenanceLog/Edit/5
        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var maintenanceLog = await _context.MaintenanceLogs
                .Include(m => m.ITAsset)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenanceLog == null) return NotFound();

            ViewBag.AssetTag = maintenanceLog.ITAsset?.AssetTag;
            return View(maintenanceLog);
        }

        // POST: MaintenanceLog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Runs the Edit operation.
        /// </summary>
        public async Task<IActionResult> Edit(int id, [Bind("Id,ITAssetId,ServiceDate,Description,ServiceProvider,Cost,Notes")] MaintenanceLog maintenanceLog)
        {
            if (id != maintenanceLog.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maintenanceLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaintenanceLogExists(maintenanceLog.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Details", "ITAsset", new { id = maintenanceLog.ITAssetId });
            }
            return View(maintenanceLog);
        }

        // GET: MaintenanceLog/Delete/5
        /// <summary>
        /// Deletes data for the Delete operation.
        /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var maintenanceLog = await _context.MaintenanceLogs
                .Include(m => m.ITAsset)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maintenanceLog == null) return NotFound();

            return View(maintenanceLog);
        }

        // POST: MaintenanceLog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        /// <summary>
        /// Deletes data for the DeleteConfirmed operation.
        /// </summary>
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maintenanceLog = await _context.MaintenanceLogs.FindAsync(id);
            if (maintenanceLog != null)
            {
                int assetId = maintenanceLog.ITAssetId;
                _context.MaintenanceLogs.Remove(maintenanceLog);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "ITAsset", new { id = assetId });
            }
            return RedirectToAction("Index", "ITAsset");
        }

        /// <summary>
        /// Runs the MaintenanceLogExists operation.
        /// </summary>
        private bool MaintenanceLogExists(int id)
        {
            return _context.MaintenanceLogs.Any(e => e.Id == id);
        }
    }
}

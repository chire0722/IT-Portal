using IT_Form.Data;
using IT_Form.Application.Features.AuditLogs.Shared;
using IT_Form.Models;
using IT_Form.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Controllers
{
    [Authorize(Policy = "RequireHROrIT")]
    public class AuditLogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AuditLog
        public async Task<IActionResult> Index(string? entity, string? auditAction, string? search, DateTime? startDate, DateTime? endDate, bool? isSuccess, int page = 1)
        {
            const int pageSize = 25;

            var query = BuildFilteredAuditQuery(entity, auditAction, search, startDate, endDate, isSuccess);
            var paginatedList = await Helpers.PaginatedList<AuditLog>.CreateAsync(
                query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Timestamp), page, pageSize);

            var filteredFailureCount = await query.CountAsync(a => !a.IsSuccess);
            var model = new AuditLogIndexPageViewModel
            {
                Entries = paginatedList,
                Entity = entity,
                AuditAction = auditAction,
                Search = search,
                IsSuccess = isSuccess,
                StartDate = startDate?.ToString("yyyy-MM-dd"),
                EndDate = endDate?.ToString("yyyy-MM-dd"),
                FilteredFailureCount = filteredFailureCount,
                FilteredSuccessCount = paginatedList.TotalCount - filteredFailureCount,
                FilteredCommandCount = await query.CountAsync(a => a.EntityName == "Command"),
                FailedCommandCount = await query.CountAsync(a => a.EntityName == "Command" && !a.IsSuccess),
                TopFailedActions = await query
                    .Where(a => !a.IsSuccess && !string.IsNullOrWhiteSpace(a.Action))
                    .GroupBy(a => a.Action)
                    .Select(g => new AuditLogActionStatViewModel
                    {
                        Action = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Action)
                    .Take(5)
                    .ToListAsync(),
                TopFailedActors = await query
                    .Where(a => !a.IsSuccess)
                    .Select(a => new
                    {
                        Actor = !string.IsNullOrWhiteSpace(a.PerformedBy) ? a.PerformedBy : a.UserId
                    })
                    .Where(a => !string.IsNullOrWhiteSpace(a.Actor))
                    .GroupBy(a => a.Actor)
                    .Select(g => new AuditLogActorStatViewModel
                    {
                        Actor = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Actor)
                    .Take(5)
                    .ToListAsync(),
                FailureTrend = await query
                    .Where(a => !a.IsSuccess)
                    .GroupBy(a => a.CreatedAt.Date)
                    .Select(g => new AuditLogTrendPointViewModel
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Date)
                    .Take(14)
                    .OrderBy(x => x.Date)
                    .ToListAsync()
            };

            return View(model);
        }

        // GET: /AuditLog/ExportCsv
        public async Task<IActionResult> ExportCsv(string? entity, string? auditAction, string? search, DateTime? startDate, DateTime? endDate, bool? isSuccess)
        {
            var query = BuildFilteredAuditQuery(entity, auditAction, search, startDate, endDate, isSuccess);

            var logs = await query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Timestamp).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Time,User Id,Performed By,Status,IP Address,Action,Entity Type,Entity ID,Details,Error Message,Payload,Changes");
            foreach (var log in logs)
            {
                var auditTime = log.CreatedAt == default ? log.Timestamp : log.CreatedAt;
                var localTime = auditTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                var details = log.Details?.Replace("\"", "\"\"") ?? "";
                var errorMessage = log.ErrorMessage?.Replace("\"", "\"\"") ?? "";
                var payload = log.Payload?.Replace("\"", "\"\"") ?? "";
                
                // Format the JSON changes to friendly text for the CSV
                var changesFriendly = !string.IsNullOrEmpty(log.Changes) 
                    ? IT_Form.Helpers.AuditFormatter.FormatChanges(log.Changes).Replace("\"", "\"\"") 
                    : "";
                    
                csv.AppendLine($"\"{localTime}\",\"{log.UserId}\",\"{log.PerformedBy}\",\"{(log.IsSuccess ? "Success" : "Failed")}\",\"{log.IpAddress}\",\"{log.Action}\",\"{log.EntityName}\",\"{log.EntityId}\",\"{details}\",\"{errorMessage}\",\"{payload}\",\"{changesFriendly}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"auditlog_export_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // GET: /AuditLog/Details/5
        public async Task<IActionResult> Details(int id, bool failuresOnly = false)
        {
            var log = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (log == null) return NotFound();

            var fullChain = log.CorrelationId.HasValue
                ? await _context.AuditLogs
                    .AsNoTracking()
                    .Where(a => a.CorrelationId == log.CorrelationId)
                    .OrderBy(a => a.CreatedAt)
                    .ThenBy(a => a.Timestamp)
                    .ThenBy(a => a.Id)
                    .ToListAsync()
                : new List<AuditLog> { log };

            var relatedEntries = failuresOnly
                ? fullChain.Where(a => !a.IsSuccess || a.Id == log.Id).ToList()
                : fullChain;

            return View(new AuditLogDetailsPageViewModel
            {
                Entry = log,
                FormattedAuditDetails = IT_Form.Helpers.AuditFormatter.FormatChanges(log.Changes ?? log.Payload),
                RelatedEntries = relatedEntries,
                FailuresOnly = failuresOnly,
                ChainTotalCount = fullChain.Count,
                ChainFailureCount = fullChain.Count(a => !a.IsSuccess)
            });
        }

        // GET: /AuditLog/Settings
        public async Task<IActionResult> Settings()
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "AuditNotificationEmail");
            if (setting == null)
            {
                setting = new SystemSetting 
                { 
                    Key = "AuditNotificationEmail", 
                    Value = "itdept@ytcprecast.com", 
                    Description = "Email address to receive notifications for Create/Update audit events." 
                };
            }
            return View(setting);
        }

        // POST: /AuditLog/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SystemSetting model)
        {
            if (ModelState.IsValid)
            {
                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "AuditNotificationEmail");
                if (setting == null)
                {
                    setting = new SystemSetting { Key = "AuditNotificationEmail" };
                    _context.SystemSettings.Add(setting);
                }

                setting.Value = model.Value?.Trim() ?? string.Empty;
                setting.Description = model.Description;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Audit notification settings updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        private IQueryable<AuditLog> BuildFilteredAuditQuery(
            string? entity,
            string? auditAction,
            string? search,
            DateTime? startDate,
            DateTime? endDate,
            bool? isSuccess)
            => AuditLogReadSupport.ApplyFilters(
                _context.AuditLogs.AsNoTracking(),
                new AuditLogFilterParameters(entity, auditAction, search, startDate, endDate, isSuccess));
    }
}

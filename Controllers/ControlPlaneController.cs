// Project comment: ControlPlaneController handles restricted runtime controls.

using System.Text;
using FluentValidation;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Controllers;

[Authorize(Policy = "RequireSuperAdmin")]
[Route("internal/runtime/policies")]
public sealed class ControlPlaneController : AppControllerBase
{
    private readonly IQuotaControl _quotaControl;
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemHealthService _systemHealthService;
    private readonly IBackupManagementService _backupManagementService;
    private readonly ISecurityCenterService _securityCenterService;
    private readonly IMaintenanceModeService _maintenanceModeService;
    private readonly IDataCleanupPreviewService _cleanupService;
    private readonly ISafeSystemSettingsService _settingsService;

    public ControlPlaneController(
        IQuotaControl quotaControl,
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ISystemHealthService systemHealthService,
        IBackupManagementService backupManagementService,
        ISecurityCenterService securityCenterService,
        IMaintenanceModeService maintenanceModeService,
        IDataCleanupPreviewService cleanupService,
        ISafeSystemSettingsService settingsService)
    {
        _quotaControl = quotaControl;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _systemHealthService = systemHealthService;
        _backupManagementService = backupManagementService;
        _securityCenterService = securityCenterService;
        _maintenanceModeService = maintenanceModeService;
        _cleanupService = cleanupService;
        _settingsService = settingsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildModelAsync(cancellationToken));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ControlPlanePageViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Quotas = await _quotaControl.GetSnapshotsAsync(cancellationToken);
            return View(model);
        }

        try
        {
            var updates = model.Items
                .Select(item => new QuotaUpdate(item.Key, item.Limit, item.ReadOnly, item.Reason))
                .ToList();

            await _quotaControl.UpdateAsync(updates, ActorName, cancellationToken);
            await WriteQuotaAuditAsync(updates, true, null, cancellationToken);
            TempData["SuccessMessage"] = "Settings updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            await WriteQuotaAuditAsync(
                model.Items.Select(item => new QuotaUpdate(item.Key, item.Limit, item.ReadOnly)),
                false,
                string.Join("; ", ex.Errors.Select(error => error.ErrorMessage)),
                cancellationToken);

            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }

            model.Quotas = await _quotaControl.GetSnapshotsAsync(cancellationToken);
            return View(model);
        }
    }

    [HttpGet("operations")]
    public async Task<IActionResult> Operations(CancellationToken cancellationToken)
    {
        return View(await BuildOperationsModelAsync(cancellationToken));
    }

    [HttpPost("operations/backup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualBackup(CancellationToken cancellationToken)
    {
        await _backupManagementService.RunManualBackupAsync(ActorName, cancellationToken);
        TempData["SuccessMessage"] = "Manual backup created.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpGet("operations/backups/{fileName}")]
    public async Task<IActionResult> DownloadBackup(string fileName, CancellationToken cancellationToken)
    {
        var payload = await _backupManagementService.GetBackupPayloadAsync(fileName, cancellationToken);
        if (payload == null)
        {
            return NotFound();
        }

        await WriteControlAuditAsync("DownloadBackup", "Backup", fileName, "Super Admin downloaded protected backup.", null, true, null, cancellationToken);
        return File(payload.Content, payload.ContentType, payload.FileDownloadName);
    }

    [HttpPost("operations/security/unlock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlockUser(string userId, CancellationToken cancellationToken)
    {
        await _securityCenterService.UnlockUserAsync(userId, ActorName, cancellationToken);
        TempData["SuccessMessage"] = "User unlocked.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("operations/security/force-password-reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForcePasswordReset(string userId, CancellationToken cancellationToken)
    {
        await _securityCenterService.ForcePasswordResetAsync(userId, ActorName, cancellationToken);
        TempData["SuccessMessage"] = "Password reset required on next login.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("operations/maintenance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMaintenance(MaintenanceModeEditModel model, CancellationToken cancellationToken)
    {
        await _maintenanceModeService.SetStateAsync(model.Enabled, model.Reason, ActorName, cancellationToken);
        TempData["SuccessMessage"] = model.Enabled ? "Maintenance mode enabled." : "Maintenance mode disabled.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("operations/cleanup")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteCleanup(CancellationToken cancellationToken)
    {
        await _cleanupService.ExecuteAsync(ActorName, cancellationToken);
        TempData["SuccessMessage"] = "Cleanup completed.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpPost("operations/settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSafeSettings(SafeSystemSettingsModel model, CancellationToken cancellationToken)
    {
        await _settingsService.SaveAsync(model, ActorName, cancellationToken);
        TempData["SuccessMessage"] = "System settings updated.";
        return RedirectToAction(nameof(Operations));
    }

    [HttpGet("operations/activity.csv")]
    public async Task<IActionResult> AdminActivityCsv(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var logs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _context.AuditLogs
                .AsNoTracking()
                .Where(log => log.CreatedAt >= since &&
                    (log.EntityName == "ControlPlane" || log.EntityName == "License" || log.Action.Contains("Admin") || log.Action.Contains("Quota") || log.Action.Contains("Maintenance") || log.Action.Contains("Backup")))
                .OrderByDescending(log => log.CreatedAt)
                .Take(1000),
            cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("CreatedAt,PerformedBy,Action,EntityName,EntityId,IsSuccess,Details");
        foreach (var log in logs)
        {
            csv.AppendLine(string.Join(",", Csv(log.CreatedAt.ToString("O")), Csv(log.PerformedBy), Csv(log.Action), Csv(log.EntityName), Csv(log.EntityId), Csv(log.IsSuccess.ToString()), Csv(log.Details ?? string.Empty)));
        }

        await WriteControlAuditAsync("ExportAdminActivity", "ControlPlane", "AdminActivity", "Super Admin exported admin activity report.", null, true, null, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"admin-activity-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private async Task<ControlPlanePageViewModel> BuildModelAsync(CancellationToken cancellationToken)
    {
        var snapshots = await _quotaControl.GetSnapshotsAsync(cancellationToken);
        return new ControlPlanePageViewModel
        {
            Quotas = snapshots,
            Items = snapshots
                .Select(snapshot => new QuotaEditItem
                {
                    Key = snapshot.Key,
                    Label = snapshot.Label,
                    Limit = snapshot.Limit,
                    ReadOnly = snapshot.ReadOnly,
                    FrozenLimit = snapshot.FrozenLimit,
                    CurrentUsage = snapshot.CurrentUsage,
                    Reason = snapshot.Reason,
                    ChangedBy = snapshot.ChangedBy,
                    ChangedAtUtc = snapshot.ChangedAtUtc
                })
                .ToList()
        };
    }

    private async Task<OperationsCenterViewModel> BuildOperationsModelAsync(CancellationToken cancellationToken)
    {
        return new OperationsCenterViewModel
        {
            Health = await _systemHealthService.GetSummaryAsync(cancellationToken),
            Backups = await _backupManagementService.ListBackupsAsync(cancellationToken),
            Security = await _securityCenterService.GetSummaryAsync(cancellationToken),
            Maintenance = await _maintenanceModeService.GetStateAsync(cancellationToken),
            Quotas = await _quotaControl.GetSnapshotsAsync(cancellationToken),
            Cleanup = await _cleanupService.PreviewAsync(cancellationToken),
            Settings = await _settingsService.GetAsync(cancellationToken),
            AuditWatch = await BuildAuditWatchAsync(cancellationToken)
        };
    }

    private async Task<AuditWatchSummary> BuildAuditWatchAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var highRisk = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _context.AuditLogs
                .AsNoTracking()
                .Where(log => log.CreatedAt >= since &&
                    (!log.IsSuccess || log.Action.Contains("Delete") || log.Action.Contains("Quota") || log.Action.Contains("SuperAdmin") || log.Action.Contains("Maintenance") || log.Action.Contains("Backup")))
                .OrderByDescending(log => log.CreatedAt)
                .Take(25),
            cancellationToken);
        return new AuditWatchSummary(
            highRisk,
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.AuditLogs, log => log.CreatedAt >= since && !log.IsSuccess, cancellationToken),
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.AuditLogs, log => log.CreatedAt >= since && log.Action.Contains("Delete"), cancellationToken),
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.AuditLogs, log => log.CreatedAt >= since && log.Action.Contains("Quota"), cancellationToken),
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(_context.AuditLogs, log => log.CreatedAt >= since && log.Action.Contains("SuperAdmin"), cancellationToken));
    }

    private async Task WriteQuotaAuditAsync(
        IEnumerable<QuotaUpdate> updates,
        bool isSuccess,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var actor = User?.Identity?.Name ?? "System";
        var changes = System.Text.Json.JsonSerializer.Serialize(updates.Select(update => new
        {
            update.Key,
            update.Limit,
            update.ReadOnly,
            update.Reason
        }));

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            Action = "UpdateQuotaSettings",
            PerformedBy = actor,
            EntityName = "ControlPlane",
            EntityId = "Quota",
            Details = isSuccess ? "Super Admin updated quota settings." : "Super Admin quota update failed.",
            Changes = changes,
            Payload = changes,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task WriteControlAuditAsync(
        string action,
        string entityName,
        string entityId,
        string details,
        string? changes,
        bool isSuccess,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = ActorName,
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            Action = action,
            PerformedBy = ActorName,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            Changes = changes,
            Payload = changes,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private string ActorName => User?.Identity?.Name ?? "System";

    private static string Csv(string value)
        => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}

public sealed class ControlPlanePageViewModel
{
    public List<QuotaEditItem> Items { get; set; } = [];
    public IReadOnlyList<QuotaSnapshot> Quotas { get; set; } = [];
}

public sealed class QuotaEditItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Limit { get; set; }
    public bool ReadOnly { get; set; }
    public int FrozenLimit { get; set; }
    public int CurrentUsage { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime? ChangedAtUtc { get; set; }
}

public sealed class OperationsCenterViewModel
{
    public SystemHealthSummary Health { get; set; } = null!;
    public IReadOnlyList<BackupFileSummary> Backups { get; set; } = [];
    public SecurityCenterSummary Security { get; set; } = null!;
    public MaintenanceModeState Maintenance { get; set; } = null!;
    public IReadOnlyList<QuotaSnapshot> Quotas { get; set; } = [];
    public CleanupPreview Cleanup { get; set; } = null!;
    public SafeSystemSettingsModel Settings { get; set; } = null!;
    public AuditWatchSummary AuditWatch { get; set; } = null!;
}

public sealed class MaintenanceModeEditModel
{
    public bool Enabled { get; set; }
    public string? Reason { get; set; }
}

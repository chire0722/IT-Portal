// Project comment: ControlPlaneOperations.cs contains Super Admin operational support services.

using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Models;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;

public static class ControlPlaneSettingKeys
{
    public const string MaintenanceEnabled = "ControlPlane.Maintenance.Enabled";
    public const string MaintenanceReason = "ControlPlane.Maintenance.Reason";
    public const string MaintenanceChangedBy = "ControlPlane.Maintenance.ChangedBy";
    public const string MaintenanceChangedAt = "ControlPlane.Maintenance.ChangedAt";
    public const string SupportEmail = "ControlPlane.Settings.SupportEmail";
    public const string AuditNotificationEmail = "AuditNotificationEmail";
    public const string RetentionRecordDays = "ControlPlane.Settings.RetentionRecordDays";
    public const string RetentionTokenDays = "ControlPlane.Settings.RetentionTokenDays";
    public const string RetentionAuditDays = "ControlPlane.Settings.RetentionAuditDays";
    public const string OtpExpiryMinutes = "ControlPlane.Settings.OtpExpiryMinutes";
    public const string LockoutSchedule = "ControlPlane.Settings.LockoutSchedule";
}

public sealed record SystemHealthSummary(
    bool DatabaseAvailable,
    string EnvironmentName,
    string AppVersion,
    int PendingEmailCount,
    int FailedEmailCount,
    int FailedCommandCount24h,
    int AuditLogCount,
    long DatabaseSizeBytes,
    long LogSizeBytes,
    BackupFileSummary? LatestBackup,
    MaintenanceModeState Maintenance);

public sealed record BackupFileSummary(string Name, string FullPath, long SizeBytes, DateTime CreatedAtUtc);
public sealed record MaintenanceModeState(bool Enabled, string? Reason, string? ChangedBy, DateTime? ChangedAtUtc);
public sealed record SecurityCenterSummary(IReadOnlyList<LockedUserSummary> LockedUsers, int FailedLoginCount24h, int FailedOtpCount24h, IReadOnlyList<AuditLog> RecentSecurityEvents);
public sealed record LockedUserSummary(string Id, string UserName, string? FullName, string? Email, DateTimeOffset? LockoutEnd);
public sealed record AuditWatchSummary(IReadOnlyList<AuditLog> HighRiskEvents, int FailedCommandCount, int DeleteCount, int QuotaChangeCount, int SuperAdminLoginCount);
public sealed record CleanupPreview(int ExpiredOtpCount, int ExpiredSigningTokenCount, int ExpiredDeviceTokenCount, int RedactableAuditLogCount);
public sealed record SafeSystemSettingsModel(string? SupportEmail, string? AuditNotificationEmail, int RecordRetentionDays, int TokenRetentionDays, int AuditRetentionDays, int OtpExpiryMinutes, string? LockoutSchedule);

public interface ISystemHealthService
{
    Task<SystemHealthSummary> GetSummaryAsync(CancellationToken cancellationToken);
}

public interface IBackupManagementService
{
    Task<IReadOnlyList<BackupFileSummary>> ListBackupsAsync(CancellationToken cancellationToken);
    Task<BackupFileSummary> RunManualBackupAsync(string actor, CancellationToken cancellationToken);
    Task<BinaryFilePayload?> GetBackupPayloadAsync(string fileName, CancellationToken cancellationToken);
}

public interface IMaintenanceModeService
{
    Task<MaintenanceModeState> GetStateAsync(CancellationToken cancellationToken);
    Task SetStateAsync(bool enabled, string? reason, string actor, CancellationToken cancellationToken);
    Task EnsureWritableAsync(System.Security.Claims.ClaimsPrincipal? user, CancellationToken cancellationToken);
}

public interface ISecurityCenterService
{
    Task<SecurityCenterSummary> GetSummaryAsync(CancellationToken cancellationToken);
    Task UnlockUserAsync(string userId, string actor, CancellationToken cancellationToken);
    Task ForcePasswordResetAsync(string userId, string actor, CancellationToken cancellationToken);
}

public interface IDataCleanupPreviewService
{
    Task<CleanupPreview> PreviewAsync(CancellationToken cancellationToken);
    Task<CleanupPreview> ExecuteAsync(string actor, CancellationToken cancellationToken);
}

public interface ISafeSystemSettingsService
{
    Task<SafeSystemSettingsModel> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(SafeSystemSettingsModel model, string actor, CancellationToken cancellationToken);
}

public sealed class SystemHealthService : ISystemHealthService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IBackupManagementService _backupService;
    private readonly IMaintenanceModeService _maintenanceModeService;

    public SystemHealthService(ApplicationDbContext context, IWebHostEnvironment environment, IBackupManagementService backupService, IMaintenanceModeService maintenanceModeService)
    {
        _context = context;
        _environment = environment;
        _backupService = backupService;
        _maintenanceModeService = maintenanceModeService;
    }

    public async Task<SystemHealthSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        var since = DateTime.UtcNow.AddHours(-24);
        var backups = await _backupService.ListBackupsAsync(cancellationToken);
        return new SystemHealthSummary(
            canConnect,
            _environment.EnvironmentName,
            typeof(SystemHealthService).Assembly.GetName().Version?.ToString() ?? "unknown",
            await _context.EmailOutboxMessages.CountAsync(message => message.Status != EmailOutboxStatus.Sent, cancellationToken),
            await _context.EmailOutboxMessages.CountAsync(message => message.Status == EmailOutboxStatus.Failed, cancellationToken),
            await _context.AuditLogs.CountAsync(log => log.CreatedAt >= since && !log.IsSuccess, cancellationToken),
            await _context.AuditLogs.CountAsync(cancellationToken),
            GetDatabaseSizeBytes(),
            GetDirectorySize(Path.Combine(_environment.ContentRootPath, "logs")),
            backups.FirstOrDefault(),
            await _maintenanceModeService.GetStateAsync(cancellationToken));
    }

    private long GetDatabaseSizeBytes()
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return 0;
        }

        var path = Path.IsPathRooted(dataSource) ? dataSource : Path.Combine(_environment.ContentRootPath, dataSource);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static long GetDirectorySize(string path)
        => Directory.Exists(path)
            ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length)
            : 0;
}

public sealed class BackupManagementService : IBackupManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IDataProtector _protector;

    public BackupManagementService(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment environment, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
        _protector = dataProtectionProvider.CreateProtector("IT_Form.DatabaseBackups.v1");
    }

    public Task<IReadOnlyList<BackupFileSummary>> ListBackupsAsync(CancellationToken cancellationToken)
    {
        var backups = GetBackupDirectories()
            .Where(Directory.Exists)
            .SelectMany(path => new DirectoryInfo(path).GetFiles("*.protected"))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Take(25)
            .Select(file => new BackupFileSummary(file.Name, file.FullName, file.Length, file.CreationTimeUtc))
            .ToList();
        return Task.FromResult<IReadOnlyList<BackupFileSummary>>(backups);
    }

    public async Task<BackupFileSummary> RunManualBackupAsync(string actor, CancellationToken cancellationToken)
    {
        var connectionString = _context.Database.GetConnectionString() ?? throw new InvalidOperationException("Database connection string is unavailable.");
        var dbPath = ResolvePath(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!File.Exists(dbPath))
        {
            throw new InvalidOperationException("Database file was not found.");
        }

        var backupDir = GetBackupDirectories().First();
        Directory.CreateDirectory(backupDir);
        var backupBytes = await CreateSqliteBackupBytesAsync(
            BuildConnectionStringWithDataSource(connectionString, dbPath),
            backupDir,
            cancellationToken);
        var backupName = $"{Path.GetFileNameWithoutExtension(dbPath)}_{DateTime.Now:yyyyMMdd_HHmmss}.db.protected";
        var backupPath = Path.Combine(backupDir, backupName);
        await File.WriteAllBytesAsync(backupPath, _protector.Protect(backupBytes), cancellationToken);

        await WriteAuditAsync("ManualBackup", actor, $"Super Admin created manual backup {backupName}.", JsonSerializer.Serialize(new { backupName }), cancellationToken);
        var file = new FileInfo(backupPath);
        return new BackupFileSummary(file.Name, file.FullName, file.Length, file.CreationTimeUtc);
    }

    public async Task<BinaryFilePayload?> GetBackupPayloadAsync(string fileName, CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(fileName);
        var match = (await ListBackupsAsync(cancellationToken))
            .FirstOrDefault(file => string.Equals(file.Name, safeName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            return null;
        }

        return new BinaryFilePayload(await File.ReadAllBytesAsync(match.FullPath, cancellationToken), "application/octet-stream", match.Name);
    }

    private IEnumerable<string> GetBackupDirectories()
    {
        var configured = _configuration["DatabaseBackup:BackupPath"];
        yield return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(_environment.ContentRootPath, "App_Data", "backups")
            : ResolvePath(configured);

        foreach (var key in new[] { "DatabaseBackup:AdditionalBackupPath", "DatabaseBackup2:AdditionalBackupPath" })
        {
            var path = _configuration[key];
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return ResolvePath(path);
            }
        }
    }

    private string ResolvePath(string path)
    {
        const string dataDirectoryToken = "|DataDirectory|";
        if (path.Contains(dataDirectoryToken, StringComparison.OrdinalIgnoreCase))
        {
            var configuredDataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
                ? Path.Combine(_environment.ContentRootPath, "App_Data")
                : configuredDataDirectory;

            var relativePath = path[(path.IndexOf(dataDirectoryToken, StringComparison.OrdinalIgnoreCase) + dataDirectoryToken.Length)..]
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            path = Path.Combine(dataDirectory, relativePath);
        }

        return Path.IsPathRooted(path) ? path : Path.Combine(_environment.ContentRootPath, path);
    }

    private static string BuildConnectionStringWithDataSource(string connectionString, string dbPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString)
        {
            DataSource = dbPath
        };

        return builder.ToString();
    }

    private static async Task<byte[]> CreateSqliteBackupBytesAsync(
        string sourceConnectionString,
        string backupDir,
        CancellationToken cancellationToken)
    {
        var tempBackupDir = Path.Combine(backupDir, "temp");
        Directory.CreateDirectory(tempBackupDir);

        var tempBackupPath = Path.Combine(
            tempBackupDir,
            $"IT_FormDB_manual_backup_{Guid.NewGuid():N}.db");

        try
        {
            var destinationConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = tempBackupPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

            cancellationToken.ThrowIfCancellationRequested();

            using (var source = new SqliteConnection(sourceConnectionString))
            using (var destination = new SqliteConnection(destinationConnectionString))
            {
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            }

            return await File.ReadAllBytesAsync(tempBackupPath, cancellationToken);
        }
        finally
        {
            try
            {
                if (File.Exists(tempBackupPath))
                {
                    File.Delete(tempBackupPath);
                }
            }
            catch
            {
                // Leaving a temporary snapshot is preferable to failing a completed backup.
            }
        }
    }

    private async Task WriteAuditAsync(string action, string actor, string details, string changes, CancellationToken cancellationToken)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            PerformedBy = actor,
            EntityName = "ControlPlane",
            EntityId = "Backup",
            Action = action,
            Details = details,
            Changes = changes,
            Payload = changes,
            IsSuccess = true,
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class MaintenanceModeService : IMaintenanceModeService
{
    private readonly ApplicationDbContext _context;

    public MaintenanceModeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenanceModeState> GetStateAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.SystemSettings.AsNoTracking().ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
        var changedAt = settings.TryGetValue(ControlPlaneSettingKeys.MaintenanceChangedAt, out var changedAtValue) &&
            DateTime.TryParse(changedAtValue, out var parsed)
                ? parsed
                : (DateTime?)null;
        return new MaintenanceModeState(
            settings.TryGetValue(ControlPlaneSettingKeys.MaintenanceEnabled, out var enabled) && bool.TryParse(enabled, out var parsedEnabled) && parsedEnabled,
            settings.GetValueOrDefault(ControlPlaneSettingKeys.MaintenanceReason),
            settings.GetValueOrDefault(ControlPlaneSettingKeys.MaintenanceChangedBy),
            changedAt);
    }

    public async Task SetStateAsync(bool enabled, string? reason, string actor, CancellationToken cancellationToken)
    {
        await SetAsync(ControlPlaneSettingKeys.MaintenanceEnabled, enabled.ToString(), "Global maintenance mode flag.", cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.MaintenanceReason, reason ?? string.Empty, "Global maintenance mode reason.", cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.MaintenanceChangedBy, actor, "Global maintenance mode actor.", cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.MaintenanceChangedAt, DateTime.UtcNow.ToString("O"), "Global maintenance mode timestamp.", cancellationToken);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            PerformedBy = actor,
            EntityName = "ControlPlane",
            EntityId = "MaintenanceMode",
            Action = enabled ? "EnableMaintenanceMode" : "DisableMaintenanceMode",
            Details = enabled ? "Super Admin enabled maintenance mode." : "Super Admin disabled maintenance mode.",
            Changes = JsonSerializer.Serialize(new { enabled, reason }),
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureWritableAsync(System.Security.Claims.ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        var state = await GetStateAsync(cancellationToken);
        if (state.Enabled && !PortalTitles.IsSuperAdmin(user))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "Maintenance mode is enabled. You can view and export records, but changes are blocked.")]);
        }
    }

    private async Task SetAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (setting == null)
        {
            _context.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Description = description });
        }
        else
        {
            setting.Value = value;
            setting.Description = description;
        }
    }
}

public sealed class SecurityCenterService : ISecurityCenterService
{
    private readonly ApplicationDbContext _context;

    public SecurityCenterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SecurityCenterSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var since = DateTime.UtcNow.AddHours(-24);
        var usersWithLockout = await _context.Users
            .AsNoTracking()
            .Where(user => user.LockoutEnd != null)
            .Select(user => new LockedUserSummary(user.Id, user.UserName ?? string.Empty, user.FullName, user.Email, user.LockoutEnd))
            .ToListAsync(cancellationToken);
        var locked = usersWithLockout
            .Where(user => user.LockoutEnd > now)
            .OrderBy(user => user.FullName)
            .ToList();
        var recent = await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.CreatedAt >= since && (log.Action.Contains("Login") || log.Action.Contains("Otp") || log.Action.Contains("Password") || !log.IsSuccess))
            .OrderByDescending(log => log.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
        return new SecurityCenterSummary(
            locked,
            await _context.AuditLogs.CountAsync(log => log.CreatedAt >= since && log.Action.Contains("Login") && !log.IsSuccess, cancellationToken),
            await _context.OtpRecords.CountAsync(otp => otp.CreatedAt >= since && otp.FailedAttempts > 0, cancellationToken),
            recent);
    }

    public async Task UnlockUserAsync(string userId, string actor, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstAsync(item => item.Id == userId, cancellationToken);
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await WriteAuditAsync("UnlockUser", actor, user.Id, $"Super Admin unlocked {user.UserName}.", cancellationToken);
    }

    public async Task ForcePasswordResetAsync(string userId, string actor, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstAsync(item => item.Id == userId, cancellationToken);
        user.MustChangePassword = true;
        await WriteAuditAsync("ForcePasswordReset", actor, user.Id, $"Super Admin forced password reset for {user.UserName}.", cancellationToken);
    }

    private async Task WriteAuditAsync(string action, string actor, string entityId, string details, CancellationToken cancellationToken)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            PerformedBy = actor,
            EntityName = "User Account",
            EntityId = entityId,
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DataCleanupPreviewService : IDataCleanupPreviewService
{
    private readonly ApplicationDbContext _context;

    public DataCleanupPreviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CleanupPreview> PreviewAsync(CancellationToken cancellationToken)
    {
        var tokenCutoff = DateTime.UtcNow.AddDays(-30);
        var auditCutoff = DateTime.UtcNow.AddDays(-365);
        return new CleanupPreview(
            await _context.OtpRecords.CountAsync(item => item.ExpiresAt < tokenCutoff, cancellationToken),
            await _context.SigningTokens.CountAsync(item => item.ExpiresAt < tokenCutoff, cancellationToken),
            await _context.DeviceVerificationTokens.CountAsync(item => item.CreatedAt < tokenCutoff, cancellationToken),
            await _context.AuditLogs.CountAsync(item => item.CreatedAt < auditCutoff && (item.Payload != null || item.Changes != null || item.IpAddress != null), cancellationToken));
    }

    public async Task<CleanupPreview> ExecuteAsync(string actor, CancellationToken cancellationToken)
    {
        var preview = await PreviewAsync(cancellationToken);
        var tokenCutoff = DateTime.UtcNow.AddDays(-30);
        var auditCutoff = DateTime.UtcNow.AddDays(-365);
        _context.OtpRecords.RemoveRange(await _context.OtpRecords.Where(item => item.ExpiresAt < tokenCutoff).ToListAsync(cancellationToken));
        _context.SigningTokens.RemoveRange(await _context.SigningTokens.Where(item => item.ExpiresAt < tokenCutoff).ToListAsync(cancellationToken));
        _context.DeviceVerificationTokens.RemoveRange(await _context.DeviceVerificationTokens.Where(item => item.CreatedAt < tokenCutoff).ToListAsync(cancellationToken));
        var audits = await _context.AuditLogs.Where(item => item.CreatedAt < auditCutoff && (item.Payload != null || item.Changes != null || item.IpAddress != null)).ToListAsync(cancellationToken);
        foreach (var audit in audits)
        {
            audit.Payload = null;
            audit.Changes = null;
            audit.IpAddress = null;
            audit.Details = "Audit metadata retained; payload redacted by Super Admin cleanup.";
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            PerformedBy = actor,
            EntityName = "ControlPlane",
            EntityId = "Cleanup",
            Action = "ExecuteDataCleanup",
            Details = "Super Admin executed cleanup.",
            Changes = JsonSerializer.Serialize(preview),
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);
        return preview;
    }
}

public sealed class SafeSystemSettingsService : ISafeSystemSettingsService
{
    private const string DefaultLockoutSchedule = "5,15,30,120";

    private readonly ApplicationDbContext _context;

    public SafeSystemSettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SafeSystemSettingsModel> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.SystemSettings.AsNoTracking().ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        return new SafeSystemSettingsModel(
            NormalizeText(settings.GetValueOrDefault(ControlPlaneSettingKeys.SupportEmail)),
            NormalizeText(settings.GetValueOrDefault(ControlPlaneSettingKeys.AuditNotificationEmail)),
            ReadInt(settings, ControlPlaneSettingKeys.RetentionRecordDays, 2555),
            ReadInt(settings, ControlPlaneSettingKeys.RetentionTokenDays, 30),
            ReadInt(settings, ControlPlaneSettingKeys.RetentionAuditDays, 365),
            ReadInt(settings, ControlPlaneSettingKeys.OtpExpiryMinutes, 3),
            NormalizeLockoutSchedule(settings.GetValueOrDefault(ControlPlaneSettingKeys.LockoutSchedule)));
    }

    public async Task SaveAsync(SafeSystemSettingsModel model, string actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var normalizedModel = new SafeSystemSettingsModel(
            NormalizeText(model.SupportEmail),
            NormalizeText(model.AuditNotificationEmail),
            Math.Max(1, model.RecordRetentionDays),
            Math.Max(1, model.TokenRetentionDays),
            Math.Max(1, model.AuditRetentionDays),
            Math.Max(1, model.OtpExpiryMinutes),
            NormalizeLockoutSchedule(model.LockoutSchedule));

        await SetAsync(ControlPlaneSettingKeys.SupportEmail, normalizedModel.SupportEmail, cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.AuditNotificationEmail, normalizedModel.AuditNotificationEmail, cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.RetentionRecordDays, normalizedModel.RecordRetentionDays.ToString(), cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.RetentionTokenDays, normalizedModel.TokenRetentionDays.ToString(), cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.RetentionAuditDays, normalizedModel.AuditRetentionDays.ToString(), cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.OtpExpiryMinutes, normalizedModel.OtpExpiryMinutes.ToString(), cancellationToken);
        await SetAsync(ControlPlaneSettingKeys.LockoutSchedule, normalizedModel.LockoutSchedule, cancellationToken);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = actor,
            PerformedBy = actor,
            EntityName = "ControlPlane",
            EntityId = "SystemSettings",
            Action = "UpdateSafeSystemSettings",
            Details = "Super Admin updated safe system settings.",
            Changes = JsonSerializer.Serialize(normalizedModel),
            CreatedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow,
            IsSuccess = true
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SetAsync(string key, string? value, CancellationToken cancellationToken)
    {
        var normalizedValue = value ?? string.Empty;
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (setting == null)
        {
            _context.SystemSettings.Add(new SystemSetting { Key = key, Value = normalizedValue, Description = "Safe runtime setting." });
        }
        else
        {
            setting.Value = normalizedValue;
        }
    }

    private static string NormalizeText(string? value)
        => value?.Trim() ?? string.Empty;

    private static string NormalizeLockoutSchedule(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrEmpty(normalized) ? DefaultLockoutSchedule : normalized;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
        => settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;
}

// Project comment: QuotaControl.cs centralizes database-backed quota enforcement.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Security;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;

public interface IQuotaControl
{
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuotaSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(IEnumerable<QuotaUpdate> updates, string? actor = null, CancellationToken cancellationToken = default);
    Task EnsureWritableAsync(string quotaKey, System.Security.Claims.ClaimsPrincipal? user = null, CancellationToken cancellationToken = default);
    Task EnsureAvailableAsync(string quotaKey, int requestedAdditional = 1, System.Security.Claims.ClaimsPrincipal? user = null, CancellationToken cancellationToken = default);
}

public sealed record QuotaDefinition(string Key, string Label, int DefaultLimit);
public sealed record QuotaSnapshot(string Key, string Label, int Limit, bool ReadOnly, int FrozenLimit, int CurrentUsage, string? Reason, string? ChangedBy, DateTime? ChangedAtUtc);
public sealed record QuotaUpdate(string Key, int Limit, bool ReadOnly, string? Reason = null);

public sealed class QuotaControl : IQuotaControl
{
    public const string Assets = "assets";
    public const string Software = "software";
    public const string SoftwareVersions = "softwareVersions";
    public const string Companies = "companies";
    public const string Users = "users";
    public const string Departments = "departments";
    public const string EmailGroups = "emailGroups";
    public const string SharedMailboxes = "sharedMailboxes";
    public const string ServerFolders = "serverFolders";

    public const string LimitReachedMessage = "Maximum limit reached. If you want to add more, please contact Super Admin.";
    public const string ReadOnlyMessage = "Read-only mode is enabled. You can view and export records, but changes are blocked. Please contact Super Admin.";

    private const string Prefix = "ControlPlane.Quota.";
    private const string LimitSuffix = ".Limit";
    private const string ReadOnlySuffix = ".ReadOnly";
    private const string FrozenLimitSuffix = ".FrozenLimit";
    private const string ReasonSuffix = ".Reason";
    private const string ChangedBySuffix = ".ChangedBy";
    private const string ChangedAtSuffix = ".ChangedAt";

    private static readonly QuotaDefinition[] Definitions =
    [
        new(Assets, "Assets", 100),
        new(Software, "Software", 10),
        new(SoftwareVersions, "Software Versions", 20),
        new(Companies, "Companies", 3),
        new(Users, "System Users", 10),
        new(Departments, "Departments", 10),
        new(EmailGroups, "Email Groups", 25),
        new(SharedMailboxes, "Shared Mailboxes", 25),
        new(ServerFolders, "Server Folders", 25)
    ];

    private readonly ApplicationDbContext _context;

    public QuotaControl(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var definition in Definitions)
        {
            await EnsureSettingAsync(LimitKey(definition.Key), definition.DefaultLimit.ToString(), $"{definition.Label} quota limit.", cancellationToken);
            await EnsureSettingAsync(ReadOnlyKey(definition.Key), bool.FalseString, $"{definition.Label} quota read-only flag.", cancellationToken);
            await EnsureSettingAsync(FrozenLimitKey(definition.Key), definition.DefaultLimit.ToString(), $"{definition.Label} frozen quota limit.", cancellationToken);
            await EnsureSettingAsync(ReasonKey(definition.Key), string.Empty, $"{definition.Label} quota change reason.", cancellationToken);
            await EnsureSettingAsync(ChangedByKey(definition.Key), string.Empty, $"{definition.Label} quota changed-by value.", cancellationToken);
            await EnsureSettingAsync(ChangedAtKey(definition.Key), string.Empty, $"{definition.Label} quota changed timestamp.", cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuotaSnapshot>> GetSnapshotsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var settings = await _context.SystemSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        var result = new List<QuotaSnapshot>();

        foreach (var definition in Definitions)
        {
            var limit = ReadInt(settings, LimitKey(definition.Key), definition.DefaultLimit);
            var readOnly = ReadBool(settings, ReadOnlyKey(definition.Key));
            var frozenLimit = ReadInt(settings, FrozenLimitKey(definition.Key), limit);
            var usage = await CountUsageAsync(definition.Key, cancellationToken);
            var changedAt = settings.TryGetValue(ChangedAtKey(definition.Key), out var changedAtValue) &&
                DateTime.TryParse(changedAtValue, out var parsedChangedAt)
                    ? parsedChangedAt
                    : (DateTime?)null;
            result.Add(new QuotaSnapshot(
                definition.Key,
                definition.Label,
                limit,
                readOnly,
                frozenLimit,
                usage,
                settings.GetValueOrDefault(ReasonKey(definition.Key)),
                settings.GetValueOrDefault(ChangedByKey(definition.Key)),
                changedAt));
        }

        return result;
    }

    public async Task UpdateAsync(IEnumerable<QuotaUpdate> updates, string? actor = null, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var snapshots = (await GetSnapshotsAsync(cancellationToken)).ToDictionary(snapshot => snapshot.Key, StringComparer.OrdinalIgnoreCase);
        var knownKeys = Definitions.Select(definition => definition.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var update in updates.Where(update => knownKeys.Contains(update.Key)))
        {
            var snapshot = snapshots[update.Key];
            if (update.Limit < snapshot.CurrentUsage)
            {
                throw new ValidationException([new ValidationFailure(string.Empty, $"{snapshot.Label} limit cannot be below current usage ({snapshot.CurrentUsage}).")]);
            }

            await SetValueAsync(LimitKey(update.Key), update.Limit.ToString(), cancellationToken);
            await SetValueAsync(ReadOnlyKey(update.Key), update.ReadOnly.ToString(), cancellationToken);
            await SetValueAsync(ReasonKey(update.Key), update.Reason ?? string.Empty, cancellationToken);
            await SetValueAsync(ChangedByKey(update.Key), actor ?? string.Empty, cancellationToken);
            await SetValueAsync(ChangedAtKey(update.Key), DateTime.UtcNow.ToString("O"), cancellationToken);

            if (update.ReadOnly && !snapshot.ReadOnly)
            {
                await SetValueAsync(FrozenLimitKey(update.Key), update.Limit.ToString(), cancellationToken);
            }
            else if (!update.ReadOnly)
            {
                await SetValueAsync(FrozenLimitKey(update.Key), update.Limit.ToString(), cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureAvailableAsync(string quotaKey, int requestedAdditional = 1, System.Security.Claims.ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(quotaKey, cancellationToken);
        if (snapshot == null)
        {
            return;
        }

        EnsureSnapshotWritable(snapshot, user);

        if (snapshot.CurrentUsage + requestedAdditional > snapshot.Limit)
        {
            throw new ValidationException([new ValidationFailure(string.Empty, LimitReachedMessage)]);
        }
    }

    public async Task EnsureWritableAsync(string quotaKey, System.Security.Claims.ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadSnapshotAsync(quotaKey, cancellationToken);
        if (snapshot == null)
        {
            return;
        }

        EnsureSnapshotWritable(snapshot, user);
    }

    private async Task<QuotaSnapshot?> LoadSnapshotAsync(string quotaKey, CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var snapshots = await GetSnapshotsAsync(cancellationToken);
        return snapshots.FirstOrDefault(item => string.Equals(item.Key, quotaKey, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureSnapshotWritable(QuotaSnapshot snapshot, System.Security.Claims.ClaimsPrincipal? user)
    {
        if (snapshot.ReadOnly && !PortalTitles.IsSuperAdmin(user))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, ReadOnlyMessage)]);
        }
    }

    private async Task<int> CountUsageAsync(string key, CancellationToken cancellationToken)
        => key switch
        {
            Assets => await _context.ITAssets.CountAsync(cancellationToken),
            Software => await _context.StandardSoftware.CountAsync(cancellationToken),
            SoftwareVersions => await _context.SoftwareLicenses.CountAsync(cancellationToken),
            Companies => await _context.Companies.CountAsync(company => company.IsActive, cancellationToken),
            Users => await SystemUserQuotaSupport.CountableSystemUsers(_context.Users).CountAsync(cancellationToken),
            Departments => await _context.Departments.CountAsync(department => department.IsActive, cancellationToken),
            EmailGroups => await _context.EmailGroups.CountAsync(group => group.IsActive, cancellationToken),
            SharedMailboxes => await _context.SharedMailboxes.CountAsync(mailbox => mailbox.IsActive, cancellationToken),
            ServerFolders => await _context.ServerFolders.CountAsync(folder => folder.IsActive, cancellationToken),
            _ => 0
        };

    private async Task EnsureSettingAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        if (await _context.SystemSettings.AnyAsync(setting => setting.Key == key, cancellationToken))
        {
            return;
        }

        _context.SystemSettings.Add(new IT_Form.Models.SystemSetting
        {
            Key = key,
            Value = value,
            Description = description
        });
    }

    private async Task SetValueAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _context.SystemSettings.FirstAsync(item => item.Key == key, cancellationToken);
        setting.Value = value;
    }

    private static string LimitKey(string key) => Prefix + key + LimitSuffix;
    private static string ReadOnlyKey(string key) => Prefix + key + ReadOnlySuffix;
    private static string FrozenLimitKey(string key) => Prefix + key + FrozenLimitSuffix;
    private static string ReasonKey(string key) => Prefix + key + ReasonSuffix;
    private static string ChangedByKey(string key) => Prefix + key + ChangedBySuffix;
    private static string ChangedAtKey(string key) => Prefix + key + ChangedAtSuffix;
    private static int ReadInt(IReadOnlyDictionary<string, string> settings, string key, int fallback)
        => settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : fallback;
    private static bool ReadBool(IReadOnlyDictionary<string, string> settings, string key)
        => settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;
}

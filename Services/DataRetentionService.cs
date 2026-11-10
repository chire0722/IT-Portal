// Project comment: DataRetentionService.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IT_Form.Services
{
    /// <summary>
    /// Enforces retention by redacting selected identifiers in old HR/IT request records,
    /// deleting sensitive uploads, purging expired security tokens, and redacting old audit payloads.
    /// Remaining business metadata means this is not a guarantee of irreversible anonymisation.
    /// </summary>
    public class DataRetentionService : BackgroundService
    {
        private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly PdpaComplianceOptions _options;

        public DataRetentionService(
            IServiceScopeFactory scopeFactory,
            ILogger<DataRetentionService> logger,
            IWebHostEnvironment environment,
            IOptions<PdpaComplianceOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _environment = environment;
            _options = options.Value;
        }

        /// <summary>
        /// Runs the ExecuteAsync operation.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "DataRetentionService started. Record retention={RecordDays} days; token retention={TokenDays} days; audit retention={AuditDays} days.",
                RecordRetentionPeriod.TotalDays,
                SecurityTokenRetentionPeriod.TotalDays,
                AuditLogRetentionPeriod.TotalDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunRetentionPass(stoppingToken);
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
                {
                    _logger.LogError(ex, "DataRetentionService encountered an error during retention pass.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Runs the RunRetentionPass operation.
        /// </summary>
        private async Task RunRetentionPass(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow - RecordRetentionPeriod;
            var expiredSensitiveFiles = new List<string?>();
            var redacted = await RedactExpiredOffboardingAsync(db, cutoff, expiredSensitiveFiles, ct);
            redacted += await RedactExpiredOnboardingAsync(db, cutoff, ct);

            if (redacted > 0)
            {
                // Persist the redacted references first. Never delete a file while the database
                // may still retain a live reference to it if SaveChanges fails.
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "DataRetentionService: redacted {Count} records older than {Cutoff:yyyy-MM-dd}.",
                    redacted,
                    cutoff);

                DeleteExpiredSensitiveFiles(expiredSensitiveFiles);
            }
            else
            {
                _logger.LogDebug(
                    "DataRetentionService: no records require retention redaction (cutoff: {Cutoff:yyyy-MM-dd}).",
                    cutoff);
            }

            await PurgeExpiredTokensAsync(db, ct);
            await RedactExpiredAuditLogsAsync(db, ct);
            await PurgeExpiredEmailOutboxAsync(db, cutoff, ct);
        }

        /// <summary>
        /// Runs the RedactExpiredOffboardingAsync operation.
        /// </summary>
        private async Task<int> RedactExpiredOffboardingAsync(
            ApplicationDbContext db,
            DateTime cutoff,
            ICollection<string?> expiredSensitiveFiles,
            CancellationToken ct)
        {
            var staleOffboarding = await db.OffboardingRequests
                .Where(r =>
                    (r.Status == IT_Form.Models.Enums.RequestStatus.Completed ||
                     r.Status == IT_Form.Models.Enums.RequestStatus.Rejected) &&
                    (r.CompletedAt ?? r.UpdatedAt ?? r.CreatedAt) < cutoff)
                .ToListAsync(ct);

            foreach (var req in staleOffboarding)
            {
                expiredSensitiveFiles.Add(req.ResignationLetterPath);
                expiredSensitiveFiles.Add(req.HardwareDamageImagePath);
                expiredSensitiveFiles.Add(req.HardwareReturnImagePath);

                req.FullName = "[REDACTED]";
                req.EmployeeID = "[REDACTED]";
                req.EmailAddress = "redacted@redacted.com";
                req.HODEmail = "redacted@redacted.com";
                req.CreatedByUserId = null;
                req.ApprovedByUserId = null;
                req.RejectedByUserId = null;
                req.AssignedHodId = null;
                req.ProjectCode = null;
                req.ForwardToEmail = null;
                req.EmailForwardName = null;
                req.HRSignature = null;
                req.HRSignatureHmac = null;
                req.HRSignerName = null;
                req.HODSignature = null;
                req.HODSignatureHmac = null;
                req.HODSignerName = null;
                req.ITAdminSignature = null;
                req.ITAdminSignatureHmac = null;
                req.ITAdminSignerName = null;
                req.EmployeeSignature = null;
                req.EmployeeSignatureHmac = null;
                req.EmployeeSignerName = null;
                req.ResignationLetterPath = null;
                req.HardwareDamageImagePath = null;
                req.HardwareReturnImagePath = null;
                req.HardwareNotes = null;
                req.ClearanceRemarks = null;
                req.ChecklistRemarks = null;
                req.FollowUpRemarks = null;
                req.FinesRemarks = null;
                req.OtherDepartmentDetails = null;
                req.OtherBranchDetails = null;
                req.RejectionReason = null;
                req.AccessToken = Guid.NewGuid();
            }

            return staleOffboarding.Count;
        }

        /// <summary>
        /// Deletes sensitive files only after their database references have been redacted.
        /// Failed deletions are logged for operational follow-up; the record is not restored.
        /// </summary>
        private void DeleteExpiredSensitiveFiles(IEnumerable<string?> storedPaths)
        {
            foreach (var storedPath in storedPaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.Ordinal))
            {
                try
                {
                    SensitiveFileStorage.DeleteStoredFile(_environment, storedPath);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    _logger.LogError(
                        ex,
                        "DataRetentionService could not delete an expired sensitive upload. Manual cleanup is required.");
                }
            }
        }

        /// <summary>
        /// Runs the RedactExpiredOnboardingAsync operation.
        /// </summary>
        private async Task<int> RedactExpiredOnboardingAsync(
            ApplicationDbContext db,
            DateTime cutoff,
            CancellationToken ct)
        {
            var staleOnboarding = await db.OnboardingRequests
                .Where(r =>
                    (r.Status == IT_Form.Models.Enums.RequestStatus.Completed ||
                     r.Status == IT_Form.Models.Enums.RequestStatus.Rejected) &&
                    (r.CompletedAt ?? r.UpdatedAt ?? r.CreatedAt) < cutoff)
                .ToListAsync(ct);

            foreach (var req in staleOnboarding)
            {
                req.FullName = "[REDACTED]";
                req.EmployeeID = "[REDACTED]";
                req.EmailAddress = "redacted@redacted.com";
                req.ContactNumber = "[REDACTED]";
                req.HODEmail = "redacted@redacted.com";
                req.CreatedByUserId = null;
                req.RejectedByUserId = null;
                req.DeskNumber = null;
                req.RequesterSignature = null;
                req.RequesterSignatureHmac = null;
                req.RequesterSignerName = null;
                req.ITAdminSignature = null;
                req.ITAdminSignatureHmac = null;
                req.ITAdminSignerName = null;
                req.EmployeeSignature = null;
                req.EmployeeSignatureHmac = null;
                req.EmployeeSignerName = null;
                req.AssetRemarks = null;
                req.SoftwareRemarks = null;
                req.FolderRemarks = null;
                req.OtherGroupDetails = null;
                req.OtherSoftwareDetails = null;
                req.OtherBranchDetails = null;
                req.OtherDepartmentDetails = null;
                req.OtherSharedMailboxDetails = null;
                req.OtherFolderDetails = null;
                req.ChecklistRemarks = null;
                req.RejectionReason = null;
                req.AccessToken = Guid.NewGuid();
            }

            return staleOnboarding.Count;
        }

        /// <summary>
        /// Runs the PurgeExpiredTokensAsync operation.
        /// </summary>
        private async Task PurgeExpiredTokensAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var tokenCutoff = DateTime.UtcNow - SecurityTokenRetentionPeriod;
            var purged = 0;

            var staleOtps = await db.OtpRecords.Where(t => t.ExpiresAt < tokenCutoff).ToListAsync(ct);
            if (staleOtps.Count > 0)
            {
                db.OtpRecords.RemoveRange(staleOtps);
                purged += staleOtps.Count;
            }

            var staleSigning = await db.SigningTokens.Where(t => t.ExpiresAt < tokenCutoff).ToListAsync(ct);
            if (staleSigning.Count > 0)
            {
                db.SigningTokens.RemoveRange(staleSigning);
                purged += staleSigning.Count;
            }

            var staleDevice = await db.DeviceVerificationTokens.Where(t => t.CreatedAt < tokenCutoff).ToListAsync(ct);
            if (staleDevice.Count > 0)
            {
                db.DeviceVerificationTokens.RemoveRange(staleDevice);
                purged += staleDevice.Count;
            }

            if (purged > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "DataRetentionService: purged {Count} expired security tokens older than {Cutoff:yyyy-MM-dd}.",
                    purged,
                    tokenCutoff);
            }
        }

        /// <summary>
        /// Runs the RedactExpiredAuditLogsAsync operation.
        /// </summary>
        private async Task RedactExpiredAuditLogsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var auditCutoff = DateTime.UtcNow - AuditLogRetentionPeriod;
            var staleAuditLogs = await db.AuditLogs
                .Where(log => log.CreatedAt < auditCutoff &&
                              (log.Changes != null ||
                               log.Payload != null ||
                               log.IpAddress != null ||
                               log.ActorName != null ||
                               log.ErrorMessage != null ||
                               log.UserId != "[REDACTED]" ||
                               log.PerformedBy != "[REDACTED]"))
                .ToListAsync(ct);

            foreach (var log in staleAuditLogs)
            {
                log.UserId = "[REDACTED]";
                log.PerformedBy = "[REDACTED]";
                log.ActorName = null;
                log.Details = "Audit metadata retained; personal data redacted by PDPA retention policy.";
                log.Changes = null;
                log.Payload = null;
                log.ErrorMessage = null;
                log.IpAddress = null;
            }

            if (staleAuditLogs.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation(
                    "DataRetentionService: redacted {Count} audit logs older than {Cutoff:yyyy-MM-dd}.",
                    staleAuditLogs.Count,
                    auditCutoff);
            }
        }

        /// <summary>
        /// Permanently removes terminal email outbox records after their retention period.
        /// Pending and processing messages remain durable so an interrupted delivery is not
        /// silently lost; terminal records no longer need to retain recipient or message data.
        /// </summary>
        private async Task PurgeExpiredEmailOutboxAsync(ApplicationDbContext db, DateTime cutoff, CancellationToken ct)
        {
            var staleMessages = await db.EmailOutboxMessages
                .Where(message =>
                    message.CreatedAtUtc < cutoff &&
                    (message.Status == Models.Enums.EmailOutboxStatus.Sent ||
                     message.Status == Models.Enums.EmailOutboxStatus.Failed ||
                     message.Status == Models.Enums.EmailOutboxStatus.DeadLetter))
                .ToListAsync(ct);

            if (staleMessages.Count == 0)
            {
                return;
            }

            db.EmailOutboxMessages.RemoveRange(staleMessages);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "DataRetentionService: permanently removed {Count} terminal email outbox records older than {Cutoff:yyyy-MM-dd}.",
                staleMessages.Count,
                cutoff);
        }

        private TimeSpan RecordRetentionPeriod =>
            TimeSpan.FromDays(Math.Max(1, _options.RecordRetentionDays));

        private TimeSpan SecurityTokenRetentionPeriod =>
            TimeSpan.FromDays(Math.Max(1, _options.SecurityTokenRetentionDays));

        private TimeSpan AuditLogRetentionPeriod =>
            TimeSpan.FromDays(Math.Max(1, _options.AuditLogRetentionDays));
    }
}


using IT_Form.Data;
using IT_Form.Application.Common.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IT_Form.Services
{
    /// <summary>
    /// Background service that enforces the data retention policy.
    /// Runs once per day and anonymizes completed/rejected offboarding and onboarding records 
    /// older than the configured retention period (default: 2555 days / 7 years per Malaysian 
    /// Employment Act 1955 Section 61 and PDPA 2010 Principle 5 - Retention).
    /// 
    /// "Anonymization" replaces PII fields with "[REDACTED]" while preserving the record 
    /// structure for audit/statistical purposes. It does NOT hard-delete records.
    /// </summary>
    public class DataRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionService> _logger;

        // Default retention: 7 years (Malaysian Employment Act)
        private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(2555);

        // Run once per day at 02:00 local time
        private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

        public DataRetentionService(IServiceScopeFactory scopeFactory, ILogger<DataRetentionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataRetentionService started. Retention window: {Days} days.", RetentionPeriod.TotalDays);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunRetentionPass(stoppingToken);
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Host is shutting down — this is expected, exit cleanly
                    break;
                }
                catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
                {
                    _logger.LogError(ex, "DataRetentionService encountered an error during retention pass.");
                    // Wait a short back-off before retrying so we don't tight-loop on repeated errors
                    try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); } catch (OperationCanceledException) { break; }
                }
            }

        }

        private async Task RunRetentionPass(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoff = DateTime.UtcNow - RetentionPeriod;
            int anonymized = 0;

            // -----------------------------------------------------------------
            // 1. Offboarding Requests — anonymize Completed/Rejected records past cutoff
            //    LI-07 fix: Use UpdatedAt as fallback for Rejected records (CompletedAt is never set on rejection)
            // -----------------------------------------------------------------
            var staleOffboarding = await db.OffboardingRequests
                .Where(r =>
                    (r.Status == IT_Form.Models.Enums.RequestStatus.Completed ||
                     r.Status == IT_Form.Models.Enums.RequestStatus.Rejected) &&
                    (r.CompletedAt.HasValue && r.CompletedAt.Value < cutoff ||
                     !r.CompletedAt.HasValue && r.UpdatedAt < cutoff))
                .ToListAsync(ct);

            foreach (var req in staleOffboarding)
            {
                req.FullName = "[REDACTED]";
                req.EmployeeID = "[REDACTED]";
                req.EmailAddress = "redacted@redacted.com";
                req.Department = req.Department;  // retain for statistical reporting
                req.HRSignature = null;
                req.HODSignature = null;
                req.ITAdminSignature = null;
                req.EmployeeSignature = null;
                req.ResignationLetterPath = null;
                req.HardwareNotes = null;
                req.ClearanceRemarks = null;
                req.ChecklistRemarks = null;
                req.FollowUpRemarks = null;
                anonymized++;
            }

            // -----------------------------------------------------------------
            // 2. Onboarding Requests — anonymize Completed/Rejected records past cutoff
            //    LI-07 fix: Use UpdatedAt as fallback for Rejected records
            // -----------------------------------------------------------------
            var staleOnboarding = await db.OnboardingRequests
                .Where(r =>
                    (r.Status == IT_Form.Models.Enums.RequestStatus.Completed ||
                     r.Status == IT_Form.Models.Enums.RequestStatus.Rejected) &&
                    (r.CompletedAt.HasValue && r.CompletedAt.Value < cutoff ||
                     !r.CompletedAt.HasValue && r.UpdatedAt < cutoff))
                .ToListAsync(ct);

            foreach (var req in staleOnboarding)
            {
                req.FullName = "[REDACTED]";
                req.EmployeeID = "[REDACTED]";
                req.EmailAddress = "redacted@redacted.com";
                req.ContactNumber = "[REDACTED]";
                req.HODEmail = "redacted@redacted.com";
                req.RequesterSignature = null;
                req.ITAdminSignature = null;
                req.EmployeeSignature = null;
                anonymized++;
            }

            if (anonymized > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("DataRetentionService: anonymized {Count} records older than {Cutoff:yyyy-MM-dd}.", anonymized, cutoff);
            }
            else
            {
                _logger.LogDebug("DataRetentionService: no records require anonymization (cutoff: {Cutoff:yyyy-MM-dd}).", cutoff);
            }

            // -----------------------------------------------------------------
            // 3. Temporary Tokens — Purge expired tokens older than 30 days
            // -----------------------------------------------------------------
            await PurgeExpiredTokensAsync(db, ct);
        }

        private async Task PurgeExpiredTokensAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var tokenCutoff = DateTime.UtcNow.AddDays(-30);
            int purged = 0;

            // Purge OTPs
            var staleOtps = await db.OtpRecords.Where(t => t.ExpiresAt < tokenCutoff).ToListAsync(ct);
            if (staleOtps.Any()) { db.OtpRecords.RemoveRange(staleOtps); purged += staleOtps.Count; }

            // Purge Signing Tokens
            var staleSigning = await db.SigningTokens.Where(t => t.ExpiresAt < tokenCutoff).ToListAsync(ct);
            if (staleSigning.Any()) { db.SigningTokens.RemoveRange(staleSigning); purged += staleSigning.Count; }

            // Purge Device Tokens
            var staleDevice = await db.DeviceVerificationTokens.Where(t => t.CreatedAt < tokenCutoff).ToListAsync(ct);
            if (staleDevice.Any()) { db.DeviceVerificationTokens.RemoveRange(staleDevice); purged += staleDevice.Count; }

            if (purged > 0)
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("DataRetentionService: purged {Count} expired security tokens older than 30 days.", purged);
            }
        }
    }
}

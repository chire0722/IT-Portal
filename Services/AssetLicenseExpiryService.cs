using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Application.Common.Support;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Web;
using IT_Form.Helpers;

namespace IT_Form.Services
{
    public class AssetLicenseExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AssetLicenseExpiryService> _logger;
        private readonly SmtpSettings _smtpSettings;

        public AssetLicenseExpiryService(IServiceProvider serviceProvider, ILogger<AssetLicenseExpiryService> logger, Microsoft.Extensions.Options.IOptions<SmtpSettings> smtpSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AssetLicenseExpiryService started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessExpirationsAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && !CriticalExceptionDetector.IsCritical(ex))
                    {
                        _logger.LogError(ex, "Error occurred in AssetLicenseExpiryService check loop.");
                    }

                    // Check once every 24 hours
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down
            }

            _logger.LogInformation("AssetLicenseExpiryService is stopping.");
        }

        private async Task ProcessExpirationsAsync(CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var outlookService = scope.ServiceProvider.GetRequiredService<IOutlookService>();

            var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);

            // 1. Find expiring assets (IGNORING WarrantyNotifyStatus per user request for daily alerts)
            // Filter out assets where the company doesn't plan to extend warranty AND those that are deleted
            var expiringAssets = await context.ITAssets
                .Where(a => !a.IsDeleted && a.WarrantyExpiry != null && a.WarrantyExpiry <= thirtyDaysFromNow && !a.NoExtendWarranty)
                .ToListAsync(token);

            // 2. Find expiring software licenses (IGNORING ExpiryNotifyStatus per user request for daily alerts)
            // Only include active (not deleted) licenses
            var expiringLicenses = await context.SoftwareLicenses
                .Where(sl => !sl.IsDeleted && sl.ExpiryDate != null && sl.ExpiryDate <= thirtyDaysFromNow)
                .ToListAsync(token);

            if (!expiringAssets.Any() && !expiringLicenses.Any())
            {
                return; // Nothing to report
            }

            _logger.LogInformation($"Found {expiringAssets.Count} expiring assets and {expiringLicenses.Count} expiring licenses.");

            // 3. Get IT Admin emails
            var itAdmins = await context.Users
                .Include(u => u.UserTitles)
                .Where(u => u.UserTitles.Any(t => t.Title == "IT") && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email)
                .Distinct()
                .ToListAsync(token);

            if (itAdmins == null || !itAdmins.Any())
            {
                _logger.LogWarning("No IT Admins with configured email addresses found. Cannot send expiration notifications.");
                return;
            }

            string toAddresses = string.Join(";", itAdmins);

            // 4. Construct Email Body safely using professional template
            string subject = "IT Form System - [ACTION REQUIRED] Expiring Assets and Licenses Notification";
            var paramsRows = new System.Text.StringBuilder();

            if (expiringAssets.Any())
            {
                paramsRows.Append("<tr><td colspan='2' style='padding: 12px 15px; background-color: #FFF1F2; font-size: 12px; font-weight: 700; color: #BE123C; text-transform: uppercase; border-bottom: 1px solid #E2E8F0;'>Expiring IT Assets (Warranty)</td></tr>");
                foreach (var asset in expiringAssets)
                {
                    paramsRows.Append($@"
                        <tr class='params-row'>
                            <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>{HttpUtility.HtmlEncode(asset.AssetTag ?? "N/A")}</td>
                            <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HttpUtility.HtmlEncode(asset.Model ?? "N/A")} (Exp: {asset.WarrantyExpiry?.ToString("yyyy-MM-dd")})</td>
                        </tr>");
                }
            }

            if (expiringLicenses.Any())
            {
                paramsRows.Append("<tr><td colspan='2' style='padding: 12px 15px; background-color: #FFF1F2; font-size: 12px; font-weight: 700; color: #BE123C; text-transform: uppercase; border-bottom: 1px solid #E2E8F0; border-top: 1px solid #E2E8F0;'>Expiring Software Licenses</td></tr>");
                foreach (var license in expiringLicenses)
                {
                    paramsRows.Append($@"
                        <tr class='params-row'>
                            <td class='params-label' style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>{HttpUtility.HtmlEncode(license.SoftwareName ?? "N/A")}</td>
                            <td class='params-value' style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>Exp: {license.ExpiryDate?.ToString("yyyy-MM-dd")}</td>
                        </tr>");
                }
            }

            var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            string appDomainSetting = config["AppDomain"] ?? "http://localhost:5063";
            var domains = appDomainSetting.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string baseDomain = domains.FirstOrDefault(d => !d.Contains("localhost")) ?? domains.FirstOrDefault() ?? "http://localhost:5063";
            string dashboardUrl = $"{baseDomain.TrimEnd('/')}/Home/Index";

            // We construct the notification body using our shared professional helper
            string body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                "Persistent Expiry Notification",
                "The following items remain within the 30-day expiration window. This notification will be sent daily until these items are extended or resolved.",
                paramsRows.ToString(),
                dashboardUrl, 
                "Open IT Dashboard"
            );

            foreach (var asset in expiringAssets)
            {
                asset.WarrantyNotifyStatus = ExpiryNotifyStatus.DispatchPending;
            }

            foreach (var license in expiringLicenses)
            {
                license.ExpiryNotifyStatus = ExpiryNotifyStatus.DispatchPending;
            }

            await context.SaveChangesAsync(token);

            try
            {
                outlookService.DisplayEmail(toAddresses, subject, body);

                foreach (var asset in expiringAssets)
                {
                    asset.WarrantyNotifyStatus = ExpiryNotifyStatus.Sent;
                    asset.ExpiryAttemptCount = 0;
                }
                foreach (var license in expiringLicenses)
                {
                    license.ExpiryNotifyStatus = ExpiryNotifyStatus.Sent;
                    license.ExpiryAttemptCount = 0;
                }

                try
                {
                    await context.SaveChangesAsync(token);
                    _logger.LogInformation("Successfully sent daily expiration notification batch.");
                }
                catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
                {
                    _logger.LogError(
                        ex,
                        "Expiration notification email batch was sent, but the final state update failed. Records remain marked as {Status}.",
                        ExpiryNotifyStatus.DispatchPending);
                }
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                _logger.LogError(ex, "Failed to send expiration notification email. Incrementing attempt counts.");

                foreach (var asset in expiringAssets)
                {
                    asset.WarrantyNotifyStatus = ExpiryNotifyStatus.Failed;
                    asset.ExpiryAttemptCount++;
                }

                foreach (var license in expiringLicenses)
                {
                    license.ExpiryNotifyStatus = ExpiryNotifyStatus.Failed;
                    license.ExpiryAttemptCount++;
                }

                await context.SaveChangesAsync(token);
            }
        }
    }
}

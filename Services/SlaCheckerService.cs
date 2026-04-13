using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Helpers;
using IT_Form.Application.Common.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;
using System.Web;

namespace IT_Form.Services
{
    public class SlaCheckerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SlaCheckerService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan DispatchLeaseDuration = TimeSpan.FromHours(2);

        public SlaCheckerService(IServiceScopeFactory scopeFactory, ILogger<SlaCheckerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SLA Checker Background Service is starting.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessOverdueSlasAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && !CriticalExceptionDetector.IsCritical(ex))
                    {
                        _logger.LogError(ex, "Error occurred in SLA Checker Service.");
                    }

                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Host shutting down
            }

            _logger.LogInformation("SLA Checker Background Service is stopping.");
        }

        private async Task ProcessOverdueSlasAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var outlookService = scope.ServiceProvider.GetRequiredService<IOutlookService>();

            var slaSettings = await context.SlaSettings.ToListAsync(stoppingToken);

            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            string appDomainSetting = config["AppDomain"] ?? "http://localhost:5063";
            var domains = appDomainSetting.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string baseDomain = domains.FirstOrDefault(d => !d.Contains("localhost")) ?? domains.FirstOrDefault() ?? "http://localhost:5063";
            var companies = await context.Companies
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            // 1. Onboarding
            await ProcessTiersAsync(
                context,
                slaSettings,
                companies,
                "Onboarding",
                context.OnboardingRequests,
                r => r.RequesterSignedAt ?? r.RequestDate,
                r => r.Branch,
                r => r.FullName,
                outlookService,
                baseDomain,
                stoppingToken);

            // 2. Offboarding
            await ProcessTiersAsync(
                context,
                slaSettings,
                companies,
                "Offboarding",
                context.OffboardingRequests,
                r => r.HRSignedAt ?? r.RequestDate,
                r => r.Branch,
                r => r.FullName,
                outlookService,
                baseDomain,
                stoppingToken);
        }

        private async Task ProcessTiersAsync<T>(
            ApplicationDbContext context,
            List<SlaSetting> settings,
            IReadOnlyCollection<Company> companies,
            string requestType,
            IQueryable<T> dbSet,
            Func<T, DateTime> dateSelector,
            Func<T, string?> companySelector,
            Func<T, string?> nameSelector,
            IOutlookService outlook,
            string baseDomain,
            CancellationToken ct) where T : class, ISlaTrackable
        {
            var setting = settings.FirstOrDefault(s => s.RequestType == requestType);
            if (setting == null) return;

            var pendingRequests = await dbSet
                .Where(r => r.Status != RequestStatus.Draft && r.Status != RequestStatus.Completed && r.Status != RequestStatus.Rejected)
                .ToListAsync(ct);

            foreach (var req in pendingRequests)
            {
                DateTime baseDate = dateSelector(req);
                string? companyReference = companySelector(req);
                string? name = nameSelector(req);
                int reqId = req.Id;

                string detailsPath = $"/{requestType}/Details/{reqId}";
                string detailsUrl = $"{baseDomain.TrimEnd('/')}{detailsPath}";

                // --- Level 0: Primary Notification ---
                if (ShouldDispatch(req, SlaNotifyStatus.Pending, SlaNotifyStatus.PrimaryDispatchPending))
                {
                    if (CompanyBusinessHoursHelper.IsSlaOverdue(companies, companyReference, baseDate, setting.TargetDays) &&
                        !string.IsNullOrWhiteSpace(setting.NotifyEmail))
                    {
                        var subject = $"SLA OVERDUE: {requestType} #{reqId} ({name})";
                        var detailsTable = CreateSlaDetailsTable(requestType, reqId, name, baseDate, setting.TargetDays, "Overdue");
                        var body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                            "SLA Performance Alert",
                            $"The following <strong>{requestType}</strong> request has exceeded its primary SLA threshold.",
                            detailsTable,
                            detailsUrl,
                            "View Request"
                        );
                        await DispatchLevelEmailAsync(
                            context,
                            req,
                            setting.NotifyEmail,
                            subject,
                            body,
                            SlaNotifyStatus.Pending,
                            SlaNotifyStatus.PrimaryDispatchPending,
                            SlaNotifyStatus.Sent,
                            outlook,
                            ct);
                    }
                }
                // --- Level 1: Tier 1 Escalation ---
                else if (ShouldDispatch(req, SlaNotifyStatus.Sent, SlaNotifyStatus.Tier1DispatchPending))
                {
                    if (CompanyBusinessHoursHelper.IsSlaOverdue(companies, companyReference, baseDate, setting.EscalationDaysTier1) &&
                        !string.IsNullOrWhiteSpace(setting.EscalationEmailTier1))
                    {
                        var subject = $"SLA ESCALATION (Tier 1): {requestType} #{reqId} ({name})";
                        var detailsTable = CreateSlaDetailsTable(requestType, reqId, name, baseDate, setting.EscalationDaysTier1, "Tier 1 Escalation");
                        var body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                            "Internal Escalation",
                            $"<strong>ACTION REQUIRED:</strong> This request has exceeded the Tier 1 threshold. Assigned personnel must prioritize this item.",
                            detailsTable,
                            detailsUrl,
                            "Investigate Request"
                        );
                        await DispatchLevelEmailAsync(
                            context,
                            req,
                            setting.EscalationEmailTier1,
                            subject,
                            body,
                            SlaNotifyStatus.Sent,
                            SlaNotifyStatus.Tier1DispatchPending,
                            SlaNotifyStatus.Tier1Sent,
                            outlook,
                            ct);
                    }
                }
                // --- Level 2: Tier 2 Escalation ---
                else if (ShouldDispatch(req, SlaNotifyStatus.Tier1Sent, SlaNotifyStatus.Tier2DispatchPending))
                {
                    if (CompanyBusinessHoursHelper.IsSlaOverdue(companies, companyReference, baseDate, setting.EscalationDaysTier2) &&
                        !string.IsNullOrWhiteSpace(setting.EscalationEmailTier2))
                    {
                        var subject = $"SLA CRITICAL ESCALATION (Tier 2): {requestType} #{reqId} ({name})";
                        var detailsTable = CreateSlaDetailsTable(requestType, reqId, name, baseDate, setting.EscalationDaysTier2, "CRITICAL Tier 2");
                        var body = EmailTemplateHelper.GetProfessionalHtmlTemplate(
                            "Critical Performance Alert",
                            $"<strong>URGENT:</strong> This request has exceeded the Tier 2 critical threshold. Immediate management intervention is highly recommended.",
                            detailsTable,
                            detailsUrl,
                            "Immediate Review"
                        );
                        await DispatchLevelEmailAsync(
                            context,
                            req,
                            setting.EscalationEmailTier2,
                            subject,
                            body,
                            SlaNotifyStatus.Tier1Sent,
                            SlaNotifyStatus.Tier2DispatchPending,
                            SlaNotifyStatus.Tier2Sent,
                            outlook,
                            ct);
                    }
                }
            }
        }

        private string CreateSlaDetailsTable(string type, int id, string? name, DateTime date, int limit, string level)
        {
            return $@"
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Request</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HttpUtility.HtmlEncode(type)} #{id}</td>
                </tr>
                <tr style='background-color: #FBFDFF;'>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Employee</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{HttpUtility.HtmlEncode(name ?? "N/A")}</td>
                </tr>
                <tr>
                    <td style='padding: 12px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Limit (Days)</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{limit} Days</td>
                </tr>
                <tr style='background-color: #FFF1F2;'>
                    <td style='padding: 12px 15px; font-size: 14px; color: #BE123C; font-weight: 700;'>Alert Status</td>
                    <td style='padding: 12px 15px; font-size: 14px; color: #BE123C; font-weight: 700;'>{HttpUtility.HtmlEncode(level)}</td>
                </tr>";
        }

        private bool ShouldDispatch(ISlaTrackable request, SlaNotifyStatus currentStatus, SlaNotifyStatus pendingStatus)
        {
            if (request.SlaNotifyStatus == currentStatus)
            {
                return true;
            }

            if (request.SlaNotifyStatus != pendingStatus)
            {
                return false;
            }

            return !request.UpdatedAt.HasValue || request.UpdatedAt.Value <= DateTime.UtcNow - DispatchLeaseDuration;
        }

        private async Task DispatchLevelEmailAsync(
            ApplicationDbContext context,
            ISlaTrackable request,
            string email,
            string subject,
            string body,
            SlaNotifyStatus currentStatus,
            SlaNotifyStatus pendingStatus,
            SlaNotifyStatus nextStatus,
            IOutlookService outlook,
            CancellationToken cancellationToken)
        {
            request.SlaNotifyStatus = pendingStatus;
            request.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            try
            {
                outlook.DisplayEmail(email, subject, body);
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                request.SlaNotifyStatus = currentStatus;
                request.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogError(ex, "Failed to send SLA {Status} email for Request {Id} to {Email}.", nextStatus, request.Id, email);
                return;
            }

            request.SlaNotifyStatus = nextStatus;
            request.UpdatedAt = DateTime.UtcNow;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully sent SLA {Status} email for Request {Id} to {Email}.", nextStatus, request.Id, email);
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                _logger.LogError(
                    ex,
                    "SLA email for Request {Id} was sent, but the final status update to {Status} failed. The request remains lease-protected in {PendingStatus}.",
                    request.Id,
                    nextStatus,
                    pendingStatus);
            }
        }
    }
}

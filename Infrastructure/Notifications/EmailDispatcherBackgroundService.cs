// Project comment: EmailDispatcherBackgroundService.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace IT_Form.Infrastructure.Notifications
{
    /// <summary>
    /// Provides EmailDispatcherBackground service behavior for the application.
    /// </summary>
    public sealed class EmailDispatcherBackgroundService : BackgroundService
    {
        private const int BatchSize = 20;
        private const int MaxAttemptCount = 10;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailDispatcherBackgroundService> _logger;

        public EmailDispatcherBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EmailDispatcherBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Runs the StopAsync operation.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email dispatcher is stopping. Pending messages remain durable in the email outbox.");
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Runs the ExecuteAsync operation.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email dispatcher background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processedAny = await ProcessBatchAsync(stoppingToken);
                    if (!processedAny)
                    {
                        await Task.Delay(PollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
                {
                    _logger.LogError(ex, "Email dispatcher failed while processing the durable outbox.");
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }

            _logger.LogInformation("Email dispatcher background service stopped.");
        }

        /// <summary>
        /// Processes data for the ProcessBatchAsync operation.
        /// </summary>
        private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var candidateIds = await context.EmailOutboxMessages
                .AsNoTracking()
                .Where(message =>
                    message.Status != EmailOutboxStatus.Sent &&
                    message.Status != EmailOutboxStatus.DeadLetter &&
                    (message.AvailableAfterUtc == null || message.AvailableAfterUtc <= now) &&
                    (message.LockedUntilUtc == null || message.LockedUntilUtc <= now))
                .OrderBy(message => message.Id)
                .Select(message => message.Id)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            var processedAny = false;
            foreach (var messageId in candidateIds)
            {
                if (await TryProcessMessageAsync(messageId, cancellationToken))
                {
                    processedAny = true;
                }
            }

            return processedAny;
        }

        /// <summary>
        /// Runs the TryProcessMessageAsync operation.
        /// </summary>
        private async Task<bool> TryProcessMessageAsync(int messageId, CancellationToken cancellationToken)
        {
            var claimTimestamp = DateTime.UtcNow;

            using var claimScope = _serviceScopeFactory.CreateScope();
            var claimContext = claimScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var claimed = await claimContext.EmailOutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.Status != EmailOutboxStatus.Sent &&
                    message.Status != EmailOutboxStatus.DeadLetter &&
                    (message.AvailableAfterUtc == null || message.AvailableAfterUtc <= claimTimestamp) &&
                    (message.LockedUntilUtc == null || message.LockedUntilUtc <= claimTimestamp))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Status, EmailOutboxStatus.Processing)
                    .SetProperty(message => message.LockedUntilUtc, claimTimestamp + LeaseDuration)
                    .SetProperty(message => message.LastAttemptAtUtc, claimTimestamp)
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1), cancellationToken);

            if (claimed == 0)
            {
                return false;
            }

            var outboxMessage = await claimContext.EmailOutboxMessages
                .AsNoTracking()
                .SingleAsync(message => message.Id == messageId, cancellationToken);

            try
            {
                await DispatchMessageAsync(
                    EmailOutboxMessageSerializer.Deserialize(outboxMessage.MessageType, outboxMessage.PayloadJson),
                    cancellationToken);

                await MarkMessageSentAsync(messageId, cancellationToken);
                return true;
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                await MarkMessageFailedAsync(messageId, outboxMessage.AttemptCount, ex, cancellationToken);
                return true;
            }
        }

        /// <summary>
        /// Runs the MarkMessageSentAsync operation.
        /// </summary>
        private async Task MarkMessageSentAsync(int messageId, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sentAt = DateTime.UtcNow;

            await context.EmailOutboxMessages
                .Where(message => message.Id == messageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Status, EmailOutboxStatus.Sent)
                    .SetProperty(message => message.SentAtUtc, sentAt)
                    .SetProperty(message => message.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(message => message.AvailableAfterUtc, (DateTime?)null)
                    .SetProperty(message => message.LastError, (string?)null), cancellationToken);
        }

        /// <summary>
        /// Runs the MarkMessageFailedAsync operation.
        /// </summary>
        private async Task MarkMessageFailedAsync(int messageId, int attemptCount, Exception exception, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var isDeadLetter = attemptCount >= MaxAttemptCount;
            var lastError = AuditPayloadSanitizer.SanitizeExceptionMessage(exception);

            await context.EmailOutboxMessages
                .Where(message => message.Id == messageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.Status, isDeadLetter ? EmailOutboxStatus.DeadLetter : EmailOutboxStatus.Failed)
                    .SetProperty(message => message.LockedUntilUtc, (DateTime?)null)
                    .SetProperty(message => message.AvailableAfterUtc, isDeadLetter ? (DateTime?)null : now + GetRetryDelay(attemptCount))
                    .SetProperty(message => message.LastError, lastError), cancellationToken);

            _logger.LogError(
                exception,
                "Background email dispatch failed for outbox message {MessageId}. Attempt {AttemptCount} of {MaxAttemptCount}.",
                messageId,
                attemptCount,
                MaxAttemptCount);
        }

        /// <summary>
        /// Runs the DispatchMessageAsync operation.
        /// </summary>
        private async Task DispatchMessageAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var outlookService = scope.ServiceProvider.GetRequiredService<IOutlookService>();

            switch (message)
            {
                case WelcomeEmailMessage welcomeEmail:
                    outlookService.DisplayEmail(
                        welcomeEmail.Email,
                        "Welcome to the company",
                        BuildWelcomeEmailBody(welcomeEmail.EmployeeName));
                    _logger.LogInformation("SMTP welcome email sent to {Email}.", welcomeEmail.Email);
                    break;

                case OtpEmailMessage otpEmail:
                    outlookService.DisplayEmail(
                        otpEmail.Email,
                        otpEmail.Subject,
                        otpEmail.HtmlBody);
                    _logger.LogInformation("SMTP OTP email sent to {Email}.", otpEmail.Email);
                    break;

                case RenderedEmailMessage renderedEmail:
                    outlookService.DisplayEmail(
                        renderedEmail.Email,
                        renderedEmail.Subject,
                        renderedEmail.HtmlBody,
                        null,
                        null,
                        renderedEmail.Cc,
                        renderedEmail.SignatureBase64,
                        renderedEmail.BypassActiveCheck);
                    _logger.LogInformation("SMTP rendered email sent to {Email}.", renderedEmail.Email);
                    break;

                case ItLockoutAlertMessage lockoutAlert:
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var targetEmails = await context.Users
                        .AsNoTracking()
                        .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                        .Join(
                            context.UserTitles.AsNoTracking(),
                            user => user.Id,
                            title => title.UserId,
                            (user, title) => new { User = user, Title = title })
                        .Where(x => x.Title.Title == "IT")
                        .Select(x => x.User.Email!)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    if (targetEmails.Count == 0)
                    {
                        _logger.LogWarning(
                            "No active IT users found to notify about account lockout for {LockedUserId}.",
                            lockoutAlert.UserId);
                        break;
                    }

                    var subject = "IT Form Security Alert: Account Lockout";
                    var body = BuildLockoutAlertBody(
                        lockoutAlert.Email,
                        lockoutAlert.UserId,
                        lockoutAlert.Timestamp);

                    foreach (var targetEmail in targetEmails)
                    {
                        outlookService.DisplayEmail(targetEmail, subject, body);
                        _logger.LogInformation(
                            "SMTP lockout alert email sent to {TargetEmail}.",
                            targetEmail);
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported background email message type {message.GetType().Name}.");
            }
        }

        /// <summary>
        /// Gets the data required by the GetRetryDelay operation.
        /// </summary>
        private static TimeSpan GetRetryDelay(int attemptCount)
        {
            var seconds = Math.Min(300, Math.Max(5, attemptCount * 15));
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Builds the value returned by the BuildWelcomeEmailBody operation.
        /// </summary>
        private static string BuildWelcomeEmailBody(string employeeName) => $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                <h2 style='color: #2563eb;'>Welcome to the company</h2>
                <p>Hello <b>{HtmlEncoder.Default.Encode(employeeName)}</b>,</p>
                <p>Welcome aboard. Your onboarding request has been created successfully.</p>
                <p>Please follow the onboarding instructions from the IT and HR teams.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                <small>IT Asset Management System</small>
            </div>";

        /// <summary>
        /// Builds the value returned by the BuildLockoutAlertBody operation.
        /// </summary>
        private static string BuildLockoutAlertBody(string lockedUserEmail, string userId, DateTime timestamp) => $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                <h2 style='color: #dc3545;'>Security Alert: Account Lockout</h2>
                <p>An account has been locked after repeated failed sign-in attempts.</p>
                <table style='border-collapse: collapse; margin-top: 16px;'>
                    <tr><td style='padding: 4px 12px 4px 0;'><b>User ID</b></td><td>{HtmlEncoder.Default.Encode(userId)}</td></tr>
                    <tr><td style='padding: 4px 12px 4px 0;'><b>Email</b></td><td>{HtmlEncoder.Default.Encode(lockedUserEmail)}</td></tr>
                    <tr><td style='padding: 4px 12px 4px 0;'><b>Timestamp</b></td><td>{timestamp:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
                <p style='margin-top: 16px;'>Review the account activity and assist the user if necessary.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                <small>IT Asset Management System</small>
            </div>";
    }
}


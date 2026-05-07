// Project comment: LoginAccountCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Account;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Security;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IT_Form.Application.Features.Account.LoginAccount
{
    /// <summary>
    /// Handles the LoginAccountCommand command workflow.
    /// </summary>
    public sealed class LoginAccountCommandHandler : IRequestHandler<LoginAccountCommand, LoginAccountResult>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;
        private readonly IPostCommitActionQueue _postCommitActionQueue;
        private readonly IKeyedLockProvider _keyedLockProvider;
        private readonly SecuritySettings _securitySettings;
        private readonly SecureOtpGenerator _otpGenerator;
        private readonly ILogger<LoginAccountCommandHandler> _logger;

        public LoginAccountCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailBackgroundQueue emailBackgroundQueue,
            IPostCommitActionQueue postCommitActionQueue,
            IKeyedLockProvider keyedLockProvider,
            IOptions<SecuritySettings> securityOptions,
            ILogger<LoginAccountCommandHandler> logger,
            SecureOtpGenerator? otpGenerator = null)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailBackgroundQueue = emailBackgroundQueue;
            _postCommitActionQueue = postCommitActionQueue;
            _keyedLockProvider = keyedLockProvider;
            _securitySettings = securityOptions.Value;
            _otpGenerator = otpGenerator ?? new SecureOtpGenerator();
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<LoginAccountResult> Handle(LoginAccountCommand request, CancellationToken cancellationToken)
        {
            var normalizedUserName = request.Username.Trim().ToUpperInvariant();
            var userId = await _context.Users
                .AsNoTracking()
                .Where(u => u.NormalizedUserName == normalizedUserName || u.NormalizedEmail == normalizedUserName)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(userId))
            {
                AddFailedLoginAudit(
                    null,
                    request.Username,
                    request.ClientIp,
                    "Invalid username/email or password.");
                await _context.SaveChangesAsync(cancellationToken);

                return new LoginAccountResult(
                    false,
                    "Invalid username/email or password.");
            }

            return await _keyedLockProvider.ExecuteAsync(
                $"LOGIN_LOCK_{userId}",
                async ct =>
                {
                    for (var attempt = 0; attempt < 3; attempt++)
                    {
                        _context.ChangeTracker.Clear();

                        try
                        {
                            return await HandleCoreAsync(request, userId, ct);
                        }
                        catch (DbUpdateConcurrencyException ex) when (attempt < 2)
                        {
                            _logger.LogWarning(
                                ex,
                                "Concurrent login state update detected for user {UserId}. Retrying.",
                                userId);
                        }
                    }

                    throw new InvalidOperationException("Unable to update login state after repeated concurrency conflicts.");
                },
                cancellationToken);
        }

        /// <summary>
        /// Runs the HandleCoreAsync operation.
        /// </summary>
        private async Task<LoginAccountResult> HandleCoreAsync(LoginAccountCommand request, string userId, CancellationToken cancellationToken)
        {
            var maxAttempts = _securitySettings.MaxOtpAttempts;
            var lockoutSchedule = _securitySettings.PasswordLockoutMinutes is { Count: > 0 }
                ? _securitySettings.PasswordLockoutMinutes
                : [5, 15, 30, 120];
            var user = await _context.Users.SingleAsync(u => u.Id == userId, cancellationToken);

            var isSuperAdmin = await _context.UserTitles
                .AsNoTracking()
                .AnyAsync(title => title.UserId == user.Id && title.Title == PortalTitles.SuperAdmin, cancellationToken);

            if (!user.IsActive)
            {
                if (!isSuperAdmin)
                {
                    AddFailedLoginAudit(
                        user,
                        request.Username,
                        request.ClientIp,
                        "Account is deactivated.");
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return new LoginAccountResult(
                    false,
                    "Your account has been deactivated. Please contact IT.");
            }

            if (!user.EmailConfirmed)
            {
                if (!isSuperAdmin)
                {
                    AddFailedLoginAudit(
                        user,
                        request.Username,
                        request.ClientIp,
                        "Email is not verified.");
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return new LoginAccountResult(
                    false,
                    "Your email is not verified. Please contact IT.");
            }

            if (isSuperAdmin && string.IsNullOrWhiteSpace(user.Email))
            {
                return new LoginAccountResult(false, "Super Admin account requires an email address for OTP login.");
            }

            if (isSuperAdmin &&
                string.IsNullOrWhiteSpace(request.Password) &&
                (string.IsNullOrWhiteSpace(user.PasswordHash) || user.MustChangePassword))
            {
                await QueuePrivilegedLoginOtpAsync(user, request.ClientIp, cancellationToken);
                await SaveChangesWithAuditSuppressionAsync(suppressAudit: true, cancellationToken);
                return new LoginAccountResult(
                    false,
                    "OTP sent successfully.",
                    RequiresOtp: true,
                    OtpEmail: user.Email);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                if (!isSuperAdmin)
                {
                    AddFailedLoginAudit(
                        user,
                        request.Username,
                        request.ClientIp,
                        "Account is temporarily locked.");
                    await _context.SaveChangesAsync(cancellationToken);
                }

                var remaining = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                var mins = Math.Ceiling(remaining.TotalMinutes);
                return new LoginAccountResult(
                    false,
                    $"Your account is temporarily locked. Please try again in {mins} minute(s) or contact IT to reset your password.",
                    LockoutEndUtc: user.LockoutEnd.Value);
            }

            var verifyResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password ?? string.Empty);
            if (verifyResult == PasswordVerificationResult.Success ||
                verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password ?? string.Empty);
                }

                user.AccessFailedCount = 0;
                user.LockoutCount = 0;
                user.LockoutEnd = null;
                user.ConcurrencyStamp = Guid.NewGuid().ToString("N");

                if (isSuperAdmin)
                {
                    await QueuePrivilegedLoginOtpAsync(user, request.ClientIp, cancellationToken);
                    await SaveChangesWithAuditSuppressionAsync(suppressAudit: true, cancellationToken);
                    return new LoginAccountResult(
                        false,
                        "OTP sent successfully.",
                        RequiresOtp: true,
                        OtpEmail: user.Email);
                }

                _postCommitActionQueue.Enqueue(_ => _signInManager.SignInAsync(user, request.RememberMe));

                _logger.LogInformation("User {Username} logged in successfully.", request.Username);
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow,
                    Action = "Login",
                    PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
                    EntityName = "User Account",
                    EntityId = user.Id,
                    Details = "User authenticated successfully.",
                    IsSuccess = true
                });

                await _context.SaveChangesAsync(cancellationToken);

                return new LoginAccountResult(
                    true,
                    "User authenticated successfully.",
                    MustChangePassword: user.MustChangePassword);
            }

            user.AccessFailedCount += 1;
            int attemptsLeft = maxAttempts - user.AccessFailedCount;
            if (user.AccessFailedCount >= maxAttempts)
            {
                user.LockoutCount += 1;
                var penaltyIndex = Math.Min(user.LockoutCount, lockoutSchedule.Count) - 1;
                var penaltyMinutes = lockoutSchedule[penaltyIndex];

                var lockoutTimestamp = DateTime.UtcNow;
                var shouldQueueLockoutAlert = !string.IsNullOrWhiteSpace(user.Email) &&
                    (!user.LastLockoutAlertSentAt.HasValue ||
                     user.LastLockoutAlertSentAt.Value.AddHours(1) <= lockoutTimestamp);

                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(penaltyMinutes);
                user.AccessFailedCount = 0;
                if (shouldQueueLockoutAlert)
                {
                    user.LastLockoutAlertSentAt = lockoutTimestamp;
                    await _emailBackgroundQueue.QueueEmailAsync(new ItLockoutAlertMessage(
                        user.Email!,
                        user.UserName ?? user.Id,
                        lockoutTimestamp), cancellationToken);
                }
                user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
                if (!isSuperAdmin)
                {
                    AddFailedLoginAudit(
                        user,
                        request.Username,
                        request.ClientIp,
                        $"Account locked after {maxAttempts} failed login attempts.");
                }

                await SaveChangesWithAuditSuppressionAsync(isSuperAdmin, cancellationToken);

                if (!shouldQueueLockoutAlert)
                {
                    _logger.LogInformation(
                        "Skipping lockout notification for {Username}; recent alert exists.",
                        user.UserName);
                }

                _logger.LogWarning("User {Username} locked out for {Min} min (lockout #{Count}).",
                    request.Username, penaltyMinutes, user.LockoutCount);

                return new LoginAccountResult(
                    false,
                    $"Your account has been locked due to {maxAttempts} failed login attempts. Please try again in {penaltyMinutes} minute(s) or contact IT to reset your password.",
                    LockoutEndUtc: user.LockoutEnd.Value);
            }

            user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            if (!isSuperAdmin)
            {
                AddFailedLoginAudit(
                    user,
                    request.Username,
                    request.ClientIp,
                    "Invalid username or password.");
            }

            await SaveChangesWithAuditSuppressionAsync(isSuperAdmin, cancellationToken);

            return new LoginAccountResult(
                false,
                $"Invalid username or password. You have {attemptsLeft} attempt(s) remaining before your account is locked.");
        }

        private async Task QueuePrivilegedLoginOtpAsync(ApplicationUser user, string clientIp, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException("Super Admin email is required for OTP login.");
            }

            var now = DateTime.UtcNow;
            var cooldownCutoff = now - AccountOtpSupport.RequestCooldown;
            var lastRequestedAtUtc = await _context.OtpRecords
                .AsNoTracking()
                .Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.PrivilegedLogin)
                .OrderByDescending(otp => otp.CreatedAt)
                .Select(otp => (DateTime?)otp.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRequestedAtUtc.HasValue && lastRequestedAtUtc.Value > cooldownCutoff)
            {
                throw AccountOtpSupport.CreateValidationException("Please wait 1 minute before requesting another OTP.");
            }

            await _context.OtpRecords
                .Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.PrivilegedLogin && !otp.IsUsed && !otp.IsLocked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(otp => otp.IsUsed, true)
                    .SetProperty(otp => otp.IsLocked, true), cancellationToken);

            var code = _otpGenerator.GenerateCode(AccountOtpSupport.OtpLength);
            _context.OtpRecords.Add(new OtpRecord
            {
                Email = user.Email,
                Code = AccountOtpSupport.HashCode(code),
                Purpose = OtpPurposes.PrivilegedLogin,
                ExpiresAt = now.Add(AccountOtpSupport.OtpLifetime),
                IpAddress = clientIp,
                CreatedAt = now
            });

            await _emailBackgroundQueue.QueueEmailAsync(new OtpEmailMessage(
                user.Email,
                AccountOtpSupport.BuildOtpEmailSubject(),
                AccountOtpSupport.BuildOtpEmailBody(code)), cancellationToken);
        }

        private void AddFailedLoginAudit(
            ApplicationUser? user,
            string attemptedUsername,
            string clientIp,
            string failureReason)
        {
            var auditTime = DateTime.UtcNow;
            var actor = string.IsNullOrWhiteSpace(attemptedUsername)
                ? "Unknown"
                : attemptedUsername.Trim();

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user?.Id ?? actor,
                CreatedAt = auditTime,
                Timestamp = auditTime,
                Action = "Login",
                PerformedBy = user?.FullName ?? user?.UserName ?? actor,
                EntityName = "User Account",
                EntityId = user?.Id ?? actor,
                Details = $"Failed login attempt for {actor}.",
                ErrorMessage = failureReason,
                IsSuccess = false,
                IpAddress = clientIp
            });
        }

        private async Task SaveChangesWithAuditSuppressionAsync(bool suppressAudit, CancellationToken cancellationToken)
        {
            if (!suppressAudit)
            {
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            var previousSuppressAudit = _context.SuppressAuditLogging;
            _context.SuppressAuditLogging = true;
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _context.SuppressAuditLogging = previousSuppressAudit;
            }
        }
    }
}

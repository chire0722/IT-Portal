// Project comment: LoginAccountCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
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
        private readonly ILogger<LoginAccountCommandHandler> _logger;

        public LoginAccountCommandHandler(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailBackgroundQueue emailBackgroundQueue,
            IPostCommitActionQueue postCommitActionQueue,
            IKeyedLockProvider keyedLockProvider,
            IOptions<SecuritySettings> securityOptions,
            ILogger<LoginAccountCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailBackgroundQueue = emailBackgroundQueue;
            _postCommitActionQueue = postCommitActionQueue;
            _keyedLockProvider = keyedLockProvider;
            _securitySettings = securityOptions.Value;
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

            if (!user.IsActive)
            {
                return new LoginAccountResult(
                    false,
                    "Your account has been deactivated. Please contact IT.");
            }

            if (!user.EmailConfirmed)
            {
                return new LoginAccountResult(
                    false,
                    "Your email is not verified. Please contact IT.");
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                var remaining = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                var mins = Math.Ceiling(remaining.TotalMinutes);
                return new LoginAccountResult(
                    false,
                    $"Your account is temporarily locked. Please try again in {mins} minute(s) or contact IT to reset your password.",
                    LockoutEndUtc: user.LockoutEnd.Value);
            }

            var verifyResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
            if (verifyResult == PasswordVerificationResult.Success ||
                verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
                }

                user.AccessFailedCount = 0;
                user.LockoutCount = 0;
                user.LockoutEnd = null;
                user.ConcurrencyStamp = Guid.NewGuid().ToString("N");

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
                await _context.SaveChangesAsync(cancellationToken);

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
            await _context.SaveChangesAsync(cancellationToken);

            return new LoginAccountResult(
                false,
                $"Invalid username or password. You have {attemptsLeft} attempt(s) remaining before your account is locked.");
        }
    }
}

// Project comment: ProfilePasswordChangeCommandHandlers.cs handles authenticated profile password changes with OTP confirmation.

using System.Text.Json;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Security;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;

public sealed class BeginProfilePasswordChangeCommandHandler : IRequestHandler<BeginProfilePasswordChangeCommand, BeginProfilePasswordChangeResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly SecureOtpGenerator _otpGenerator;
    private readonly IEmailBackgroundQueue _emailBackgroundQueue;
    private readonly IKeyedLockProvider _keyedLockProvider;
    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<BeginProfilePasswordChangeCommandHandler> _logger;

    public BeginProfilePasswordChangeCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        SecureOtpGenerator otpGenerator,
        IEmailBackgroundQueue emailBackgroundQueue,
        IKeyedLockProvider keyedLockProvider,
        IMemoryCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<BeginProfilePasswordChangeCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _otpGenerator = otpGenerator;
        _emailBackgroundQueue = emailBackgroundQueue;
        _keyedLockProvider = keyedLockProvider;
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("IT_Form.ProfilePasswordChange.v1");
        _logger = logger;
    }

    public async Task<BeginProfilePasswordChangeResult> Handle(BeginProfilePasswordChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw AccountOtpSupport.CreateValidationException("Your account does not have an email address. Please contact IT.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            throw new FluentValidation.ValidationException([new ValidationFailure(nameof(request.CurrentPassword), "Current password is incorrect.")]);
        }

        return await _keyedLockProvider.ExecuteAsync(
            AccountOtpSupport.BuildOperationKey(user.Id),
            async ct =>
            {
                var now = DateTime.UtcNow;
                var cooldownCutoff = now - AccountOtpSupport.RequestCooldown;
                var lastRequestedAtUtc = await _context.OtpRecords
                    .AsNoTracking()
                    .Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.ProfilePasswordChange)
                    .OrderByDescending(otp => otp.CreatedAt)
                    .Select(otp => (DateTime?)otp.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (lastRequestedAtUtc.HasValue && lastRequestedAtUtc.Value > cooldownCutoff)
                {
                    throw AccountOtpSupport.CreateValidationException("Please wait 1 minute before requesting another password-change code.");
                }

                var activeOtpRecords = await _context.OtpRecords
                    .Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.ProfilePasswordChange && !otp.IsUsed && !otp.IsLocked)
                    .ToListAsync(ct);
                foreach (var activeOtp in activeOtpRecords)
                {
                    activeOtp.IsUsed = true;
                    activeOtp.IsLocked = true;
                }

                var requestId = Guid.NewGuid().ToString("N");
                var pending = new PendingProfilePasswordChange(user.Id, request.NewPassword, now);
                _cache.Set(
                    BuildPendingCacheKey(user.Id, requestId),
                    _protector.Protect(JsonSerializer.Serialize(pending)),
                    new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = AccountOtpSupport.OtpLifetime });

                var code = _otpGenerator.GenerateCode(AccountOtpSupport.OtpLength);
                _context.OtpRecords.Add(new OtpRecord
                {
                    Email = user.Email,
                    Code = AccountOtpSupport.HashCode(code),
                    Purpose = OtpPurposes.ProfilePasswordChange,
                    ExpiresAt = now.Add(AccountOtpSupport.OtpLifetime),
                    IpAddress = request.ClientIp,
                    CreatedAt = now
                });

                await _emailBackgroundQueue.QueueEmailAsync(new OtpEmailMessage(
                    user.Email,
                    "IT Form - Confirm Password Change",
                    BuildPasswordChangeOtpEmailBody(user.FullName, code)), ct);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Queued profile password-change OTP for user {UserId}.", user.Id);
                return new BeginProfilePasswordChangeResult(requestId, user.Email);
            },
            cancellationToken);
    }

    internal static string BuildPendingCacheKey(string userId, string requestId)
        => $"profile-password-change:{userId}:{requestId}";

    private static string BuildPasswordChangeOtpEmailBody(string? fullName, string code) => $@"
            <div style='font-family: sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                <h2 style='color: #2563eb;'>Confirm Password Change</h2>
                <p>Hello <b>{System.Net.WebUtility.HtmlEncode(fullName ?? "User")}</b>,</p>
                <p>Use this one-time code to confirm your profile password change:</p>
                <div style='background: #f3f4f6; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #111; margin: 20px 0;'>
                    {code}
                </div>
                <p style='color: #666; font-size: 14px;'>This code expires in <b>5 minutes</b>. If you did not request this change, contact IT immediately.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;' />
                <small>IT Asset Management System</small>
            </div>";

    internal sealed record PendingProfilePasswordChange(string UserId, string NewPassword, DateTime CreatedAtUtc);
}

public sealed class ConfirmProfilePasswordChangeCommandHandler : IRequestHandler<ConfirmProfilePasswordChangeCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailBackgroundQueue _emailBackgroundQueue;
    private readonly IKeyedLockProvider _keyedLockProvider;
    private readonly IMemoryCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<ConfirmProfilePasswordChangeCommandHandler> _logger;

    public ConfirmProfilePasswordChangeCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IEmailBackgroundQueue emailBackgroundQueue,
        IKeyedLockProvider keyedLockProvider,
        IMemoryCache cache,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ConfirmProfilePasswordChangeCommandHandler> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailBackgroundQueue = emailBackgroundQueue;
        _keyedLockProvider = keyedLockProvider;
        _cache = cache;
        _protector = dataProtectionProvider.CreateProtector("IT_Form.ProfilePasswordChange.v1");
        _logger = logger;
    }

    public async Task<Unit> Handle(ConfirmProfilePasswordChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.UserId);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw AccountOtpSupport.CreateValidationException("Your account does not have an email address. Please contact IT.");
        }

        var cacheKey = BeginProfilePasswordChangeCommandHandler.BuildPendingCacheKey(user.Id, request.RequestId);
        if (!_cache.TryGetValue(cacheKey, out string? protectedPayload) || string.IsNullOrWhiteSpace(protectedPayload))
        {
            throw AccountOtpSupport.CreateValidationException("This password-change request has expired. Please start again.");
        }

        var pending = JsonSerializer.Deserialize<BeginProfilePasswordChangeCommandHandler.PendingProfilePasswordChange>(
            _protector.Unprotect(protectedPayload));
        if (pending == null || pending.UserId != user.Id)
        {
            _cache.Remove(cacheKey);
            throw AccountOtpSupport.CreateValidationException("Invalid password-change request.");
        }

        await _keyedLockProvider.ExecuteAsync(
            AccountOtpSupport.BuildOperationKey(user.Id),
            async ct =>
            {
                await ValidateOtpAsync(user, request.Code, ct);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, pending.NewPassword);
                result.EnsureSucceeded();

                user.MustChangePassword = false;
                (await _userManager.UpdateAsync(user)).EnsureSucceeded();
                _cache.Remove(cacheKey);
                _cache.Remove($"claims-transform:{user.Id}");

                var auditTime = DateTime.UtcNow;
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    CreatedAt = auditTime,
                    Timestamp = auditTime,
                    Action = "Change Password (Profile OTP)",
                    PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
                    EntityName = "User Account",
                    EntityId = user.Id,
                    Details = "User changed their password from profile after current-password and OTP verification.",
                    IsSuccess = true
                });
                await QueueSecurityAlertAsync(user, ct);
                await _context.SaveChangesAsync(ct);

                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("User {UserId} changed password from profile after OTP verification.", user.Id);
            },
            cancellationToken);

        return Unit.Value;
    }

    private async Task ValidateOtpAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var otpRecord = await _context.OtpRecords
            .AsNoTracking()
            .Where(otp => otp.Email == user.Email && otp.Purpose == OtpPurposes.ProfilePasswordChange && !otp.IsUsed && !otp.IsLocked)
            .OrderByDescending(otp => otp.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otpRecord == null || otpRecord.ExpiresAt < now)
        {
            throw AccountOtpSupport.CreateValidationException("Verification code has expired or does not exist.");
        }

        if (!AccountOtpSupport.MatchesStoredCode(code, otpRecord.Code))
        {
            var trackedOtp = await _context.OtpRecords
                .FirstOrDefaultAsync(otp => otp.Id == otpRecord.Id && !otp.IsUsed && !otp.IsLocked && otp.ExpiresAt >= now, cancellationToken);
            if (trackedOtp == null)
            {
                throw AccountOtpSupport.CreateValidationException("Verification code has expired or does not exist.");
            }

            trackedOtp.FailedAttempts += 1;
            if (trackedOtp.FailedAttempts >= AccountOtpSupport.MaxFailedAttempts)
            {
                trackedOtp.IsLocked = true;
                trackedOtp.IsUsed = true;
                await _context.SaveChangesAsync(cancellationToken);
                throw AccountOtpSupport.CreateValidationException("Too many incorrect attempts. Please start the password change again.");
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw AccountOtpSupport.CreateValidationException($"Verification code is incorrect. You can try {AccountOtpSupport.MaxFailedAttempts - trackedOtp.FailedAttempts} more time(s).");
        }

        var otpToConsume = await _context.OtpRecords
            .FirstOrDefaultAsync(otp => otp.Id == otpRecord.Id && otp.Purpose == OtpPurposes.ProfilePasswordChange && !otp.IsUsed && !otp.IsLocked && otp.ExpiresAt >= now, cancellationToken);
        if (otpToConsume == null)
        {
            throw AccountOtpSupport.CreateValidationException("Verification code has expired or does not exist.");
        }

        otpToConsume.IsUsed = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task QueueSecurityAlertAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string subject = "IT Form - Security Alert: Password Changed";
        var body = $@"
                <div style='font-family: sans-serif; padding: 20px; border: 1px solid #f87171; border-radius: 8px; background: #fffafb;'>
                    <h2 style='color: #dc2626;'>Security Alert</h2>
                    <p>Hello <b>{System.Net.WebUtility.HtmlEncode(user.FullName)}</b>,</p>
                    <p>Your IT Portal password was changed from your profile after OTP confirmation.</p>
                    <p><b>Time:</b> {DateTime.UtcNow.AddHours(8):dd/MM/yyyy HH:mm:ss} (SGT)</p>
                    <div style='background: #fee2e2; padding: 15px; border-radius: 4px; border: 1px solid #fecaca; margin: 20px 0;'>
                        <p style='margin: 0; color: #991b1b; font-weight: bold;'>If you did not make this change, please contact IT immediately.</p>
                    </div>
                </div>";

        await _emailBackgroundQueue.QueueEmailAsync(new RenderedEmailMessage(user.Email!, subject, body), cancellationToken);
    }
}

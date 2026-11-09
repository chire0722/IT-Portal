// Project comment: VerifyOtpCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using System.Security.Cryptography;
using IT_Form.Application.Features.Account;
using IT_Form.Data;
using IT_Form.Security;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Account.VerifyOtp
{
    /// <summary>
    /// Handles the VerifyOtpCommand command workflow.
    /// </summary>
    public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, VerifyOtpResult>
    {
        private readonly UserManager<IT_Form.Models.ApplicationUser> _userManager;
        private readonly SignInManager<IT_Form.Models.ApplicationUser>? _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IKeyedLockProvider _keyedLockProvider;
        private readonly IPostCommitActionQueue? _postCommitActionQueue;
        private readonly ILogger<VerifyOtpCommandHandler> _logger;

        public VerifyOtpCommandHandler(
            UserManager<IT_Form.Models.ApplicationUser> userManager,
            SignInManager<IT_Form.Models.ApplicationUser> signInManager,
            ApplicationDbContext context,
            IKeyedLockProvider keyedLockProvider,
            IPostCommitActionQueue postCommitActionQueue,
            ILogger<VerifyOtpCommandHandler> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _keyedLockProvider = keyedLockProvider;
            _postCommitActionQueue = postCommitActionQueue;
            _logger = logger;
        }

        public VerifyOtpCommandHandler(
            UserManager<IT_Form.Models.ApplicationUser> userManager,
            ApplicationDbContext context,
            IKeyedLockProvider keyedLockProvider,
            ILogger<VerifyOtpCommandHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _keyedLockProvider = keyedLockProvider;
            _logger = logger;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<VerifyOtpResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email)
                ?? await _userManager.FindByNameAsync(request.Email);
            if (user == null)
            {
                return new VerifyOtpResult(
                    false,
                    false,
                    VerifyOtpResult.GenericFailureMessage);
            }

            if (!user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                return new VerifyOtpResult(false, false, VerifyOtpResult.GenericFailureMessage);
            }

            return await _keyedLockProvider.ExecuteAsync(
                AccountOtpSupport.BuildOperationKey(user.Id),
                async ct =>
                {
                    var now = DateTime.UtcNow;
                    var otpRecord = await _context.OtpRecords
                        .AsNoTracking()
                        .Where(otp => otp.Email == user.Email && otp.Purpose == request.Purpose && !otp.IsUsed && !otp.IsLocked)
                        .OrderByDescending(otp => otp.CreatedAt)
                        .FirstOrDefaultAsync(ct);

                    if (otpRecord == null || otpRecord.ExpiresAt < now)
                    {
                        throw AccountOtpSupport.CreateValidationException(VerifyOtpResult.GenericFailureMessage);
                    }

                    if (!AccountOtpSupport.MatchesStoredCode(request.Code, otpRecord.Code))
                    {
                        var updatedRows = await _context.OtpRecords
                            .Where(otp =>
                                otp.Id == otpRecord.Id &&
                                !otp.IsUsed &&
                                !otp.IsLocked &&
                                otp.ExpiresAt >= now)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(otp => otp.FailedAttempts, otp => otp.FailedAttempts + 1), ct);

                        if (updatedRows == 0)
                        {
                            throw AccountOtpSupport.CreateValidationException(VerifyOtpResult.GenericFailureMessage);
                        }

                        var refreshedState = await _context.OtpRecords
                            .AsNoTracking()
                            .Where(otp => otp.Id == otpRecord.Id)
                            .Select(otp => new { otp.FailedAttempts })
                            .SingleAsync(ct);

                        if (refreshedState.FailedAttempts >= AccountOtpSupport.MaxFailedAttempts)
                        {
                            await _context.OtpRecords
                                .Where(otp => otp.Id == otpRecord.Id)
                                .ExecuteUpdateAsync(setters => setters
                                    .SetProperty(otp => otp.IsLocked, true)
                                    .SetProperty(otp => otp.IsUsed, true), ct);

                            _logger.LogWarning(
                                "OTP invalidated after {Attempts} failed attempts for user {UserId}.",
                                refreshedState.FailedAttempts,
                                user.Id);
                            throw AccountOtpSupport.CreateValidationException(VerifyOtpResult.GenericFailureMessage);
                        }

                        throw AccountOtpSupport.CreateValidationException(VerifyOtpResult.GenericFailureMessage);
                    }

                    var consumedRows = await _context.OtpRecords
                        .Where(otp =>
                            otp.Id == otpRecord.Id &&
                            otp.Purpose == request.Purpose &&
                            !otp.IsUsed &&
                            !otp.IsLocked &&
                            otp.ExpiresAt >= now)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(otp => otp.IsUsed, true), ct);

                    if (consumedRows == 0)
                    {
                        throw AccountOtpSupport.CreateValidationException("\u9a8c\u8bc1\u7801\u5df2\u8fc7\u671f\u6216\u4e0d\u5b58\u5728");
                    }

                    if (request.Purpose == OtpPurposes.PrivilegedLogin)
                    {
                        var isSuperAdmin = await _context.UserTitles
                            .AsNoTracking()
                            .AnyAsync(title => title.UserId == user.Id && title.Title == PortalTitles.SuperAdmin, ct);
                        if (!isSuperAdmin)
                        {
                            return new VerifyOtpResult(false, false, VerifyOtpResult.GenericFailureMessage);
                        }

                        var requiresPasswordSetup =
                            user.MustChangePassword ||
                            string.IsNullOrWhiteSpace(user.PasswordHash);

                        user.AccessFailedCount = 0;
                        user.LockoutEnd = null;
                        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");

                        var previousSuppressAudit = _context.SuppressAuditLogging;
                        _context.SuppressAuditLogging = true;
                        try
                        {
                            var updateResult = await _userManager.UpdateAsync(user);
                            if (!updateResult.Succeeded)
                            {
                                throw new InvalidOperationException("Failed to update Super Admin account after OTP verification.");
                            }
                        }
                        finally
                        {
                            _context.SuppressAuditLogging = previousSuppressAudit;
                        }

                        if (_signInManager == null || _postCommitActionQueue == null)
                        {
                            throw new InvalidOperationException("Super Admin OTP login requires sign-in services.");
                        }

                        _postCommitActionQueue.Enqueue(_ => _signInManager.SignInAsync(user, isPersistent: false));

                        return new VerifyOtpResult(
                            true,
                            false,
                            "OTP verified.",
                            IsLoginVerified: true,
                            RequiresPasswordSetup: requiresPasswordSetup,
                            Email: user.Email);
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    return new VerifyOtpResult(
                        true,
                        false,
                        "OTP verified.",
                        token,
                        Email: user.Email);
                },
                cancellationToken);
        }
    }
}

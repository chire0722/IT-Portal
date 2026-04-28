// Project comment: VerifyOtpCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using System.Security.Cryptography;
using IT_Form.Application.Features.Account;
using IT_Form.Data;
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
        private readonly ApplicationDbContext _context;
        private readonly IKeyedLockProvider _keyedLockProvider;
        private readonly ILogger<VerifyOtpCommandHandler> _logger;

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
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new VerifyOtpResult(
                    false,
                    true,
                    "User not found.");
            }

            return await _keyedLockProvider.ExecuteAsync(
                AccountOtpSupport.BuildOperationKey(user.Id),
                async ct =>
                {
                    var now = DateTime.UtcNow;
                    var otpRecord = await _context.OtpRecords
                        .AsNoTracking()
                        .Where(otp => otp.Email == request.Email && !otp.IsUsed && !otp.IsLocked)
                        .OrderByDescending(otp => otp.CreatedAt)
                        .FirstOrDefaultAsync(ct);

                    if (otpRecord == null || otpRecord.ExpiresAt < now)
                    {
                        throw AccountOtpSupport.CreateValidationException("\u9a8c\u8bc1\u7801\u5df2\u8fc7\u671f\u6216\u4e0d\u5b58\u5728");
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
                            throw AccountOtpSupport.CreateValidationException("\u9a8c\u8bc1\u7801\u5df2\u8fc7\u671f\u6216\u4e0d\u5b58\u5728");
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
                            throw AccountOtpSupport.CreateValidationException("\u8fde\u7eed\u8f93\u9519 3 \u6b21\uff0c\u9a8c\u8bc1\u7801\u5df2\u4f5c\u5e9f\uff0c\u8bf7\u91cd\u65b0\u83b7\u53d6");
                        }

                        throw AccountOtpSupport.CreateValidationException(
                            $"\u9a8c\u8bc1\u7801\u4e0d\u6b63\u786e\uff0c\u8fd8\u53ef\u518d\u5c1d\u8bd5 {AccountOtpSupport.MaxFailedAttempts - refreshedState.FailedAttempts} \u6b21\u3002");
                    }

                    var consumedRows = await _context.OtpRecords
                        .Where(otp =>
                            otp.Id == otpRecord.Id &&
                            !otp.IsUsed &&
                            !otp.IsLocked &&
                            otp.ExpiresAt >= now)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(otp => otp.IsUsed, true), ct);

                    if (consumedRows == 0)
                    {
                        throw AccountOtpSupport.CreateValidationException("\u9a8c\u8bc1\u7801\u5df2\u8fc7\u671f\u6216\u4e0d\u5b58\u5728");
                    }

                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    return new VerifyOtpResult(
                        true,
                        false,
                        "OTP verified.",
                        token);
                },
                cancellationToken);
        }
    }
}

// Project comment: ResendOtpCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Account;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.Account.Writes.ResendOtp;

/// <summary>
/// Handles the ResendOtpCommand command workflow.
/// </summary>
public sealed class ResendOtpCommandHandler : IRequestHandler<ResendOtpCommand, ResendOtpResult>
{
    private readonly ApplicationDbContext _context;
    private readonly SecureOtpGenerator _otpGenerator;
    private readonly IEmailBackgroundQueue _emailBackgroundQueue;
    private readonly IKeyedLockProvider _keyedLockProvider;
    private readonly ILogger<ResendOtpCommandHandler> _logger;

    public ResendOtpCommandHandler(
        ApplicationDbContext context,
        SecureOtpGenerator otpGenerator,
        IEmailBackgroundQueue emailBackgroundQueue,
        IKeyedLockProvider keyedLockProvider,
        ILogger<ResendOtpCommandHandler> logger)
    {
        _context = context;
        _otpGenerator = otpGenerator;
        _emailBackgroundQueue = emailBackgroundQueue;
        _keyedLockProvider = keyedLockProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ResendOtpResult> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var normalizedIdentifier = request.Email.Trim().ToUpperInvariant();
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.NormalizedEmail == normalizedIdentifier ||
                u.NormalizedUserName == normalizedIdentifier,
                cancellationToken);

        if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            return new ResendOtpResult(
                request.Email,
                false,
                ResendOtpResult.GenericResponseMessage);
        }

        if (request.Purpose == OtpPurposes.PrivilegedLogin)
        {
            var isSuperAdmin = await _context.UserTitles
                .AsNoTracking()
                .AnyAsync(title => title.UserId == user.Id && title.Title == PortalTitles.SuperAdmin, cancellationToken);

            if (!isSuperAdmin || (!user.MustChangePassword && !string.IsNullOrWhiteSpace(user.PasswordHash)))
            {
                return new ResendOtpResult(
                    request.Email,
                    false,
                    ResendOtpResult.GenericResponseMessage);
            }
        }
        else if (request.Purpose != OtpPurposes.PasswordReset)
        {
            return new ResendOtpResult(
                request.Email,
                false,
                ResendOtpResult.GenericResponseMessage);
        }

        return await _keyedLockProvider.ExecuteAsync(
            AccountOtpSupport.BuildOperationKey(user.Id),
            async ct =>
            {
                var now = DateTime.UtcNow;
                var cooldownCutoff = now - AccountOtpSupport.RequestCooldown;
                    var lastRequestedAtUtc = await _context.OtpRecords
                        .AsNoTracking()
                        .Where(otp => otp.Email == user.Email && otp.Purpose == request.Purpose)
                    .OrderByDescending(otp => otp.CreatedAt)
                    .Select(otp => (DateTime?)otp.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (lastRequestedAtUtc.HasValue && lastRequestedAtUtc.Value > cooldownCutoff)
                {
                    _logger.LogWarning(
                        "OTP resend throttled for user {UserId} from IP {ClientIp}.",
                        user.Id,
                        request.ClientIp);
                    return new ResendOtpResult(request.Email, false, ResendOtpResult.GenericResponseMessage);
                }

                    await _context.OtpRecords
                        .Where(otp => otp.Email == user.Email && otp.Purpose == request.Purpose && !otp.IsUsed && !otp.IsLocked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(otp => otp.IsUsed, true)
                        .SetProperty(otp => otp.IsLocked, true), ct);

                var code = _otpGenerator.GenerateCode(AccountOtpSupport.OtpLength);
                    _context.OtpRecords.Add(new Models.OtpRecord
                    {
                        Email = user.Email,
                        Code = AccountOtpSupport.HashCode(code),
                        Purpose = request.Purpose,
                        ExpiresAt = now.Add(AccountOtpSupport.OtpLifetime),
                    IpAddress = request.ClientIp,
                    CreatedAt = now
                });

                await _emailBackgroundQueue.QueueEmailAsync(new OtpEmailMessage(
                    user.Email,
                    AccountOtpSupport.BuildOtpEmailSubject(),
                    AccountOtpSupport.BuildOtpEmailBody(code)), ct);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Queued OTP resend email for user {UserId}.", user.Id);

                return new ResendOtpResult(
                    request.Email,
                    true,
                    ResendOtpResult.GenericResponseMessage);
            },
            cancellationToken);
    }
}

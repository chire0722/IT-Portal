using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Account;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Account.ForgotPassword
{
    public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly SecureOtpGenerator _otpGenerator;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;
        private readonly IKeyedLockProvider _keyedLockProvider;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        public ForgotPasswordCommandHandler(
            ApplicationDbContext context,
            SecureOtpGenerator otpGenerator,
            IEmailBackgroundQueue emailBackgroundQueue,
            IKeyedLockProvider keyedLockProvider,
            ILogger<ForgotPasswordCommandHandler> logger)
        {
            _context = context;
            _otpGenerator = otpGenerator;
            _emailBackgroundQueue = emailBackgroundQueue;
            _keyedLockProvider = keyedLockProvider;
            _logger = logger;
        }

        public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == request.Username || u.Email == request.Username, cancellationToken);

            if (user == null)
            {
                return new ForgotPasswordResult(
                    false,
                    "Invalid username or email address.");
            }

            if (!user.IsActive)
            {
                return new ForgotPasswordResult(
                    false,
                    "Your account is inactive. Please contact IT.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new ForgotPasswordResult(
                    false,
                    "This account does not have a registered email address. Please contact IT.");
            }

            return await _keyedLockProvider.ExecuteAsync(
                AccountOtpSupport.BuildOperationKey(user.Id),
                async ct =>
                {
                    var now = DateTime.UtcNow;
                    var cooldownCutoff = now - AccountOtpSupport.RequestCooldown;
                    var lastRequestedAtUtc = await _context.OtpRecords
                        .AsNoTracking()
                        .Where(otp => otp.Email == user.Email)
                        .OrderByDescending(otp => otp.CreatedAt)
                        .Select(otp => (DateTime?)otp.CreatedAt)
                        .FirstOrDefaultAsync(ct);

                    if (lastRequestedAtUtc.HasValue && lastRequestedAtUtc.Value > cooldownCutoff)
                    {
                        _logger.LogWarning(
                            "OTP request throttled for user {UserId} from IP {ClientIp}.",
                            user.Id,
                            request.ClientIp);
                        throw AccountOtpSupport.CreateValidationException("\u8bf7\u6c42\u8fc7\u4e8e\u9891\u7e41\uff0c\u8bf7 1 \u5206\u949f\u540e\u518d\u8bd5");
                    }

                    await _context.OtpRecords
                        .Where(otp => otp.Email == user.Email && !otp.IsUsed && !otp.IsLocked)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(otp => otp.IsUsed, true)
                            .SetProperty(otp => otp.IsLocked, true), ct);

                    var code = _otpGenerator.GenerateCode(AccountOtpSupport.OtpLength);
                    _context.OtpRecords.Add(new Models.OtpRecord
                    {
                        Email = user.Email,
                        Code = AccountOtpSupport.HashCode(code),
                        ExpiresAt = now.Add(AccountOtpSupport.OtpLifetime),
                        IpAddress = request.ClientIp,
                        CreatedAt = now
                    });

                    await _emailBackgroundQueue.QueueEmailAsync(new OtpEmailMessage(
                        user.Email,
                        AccountOtpSupport.BuildOtpEmailSubject(),
                        AccountOtpSupport.BuildOtpEmailBody(code)), ct);
                    await _context.SaveChangesAsync(ct);

                    _logger.LogInformation("Queued OTP email for password reset user {UserId}.", user.Id);

                    return new ForgotPasswordResult(
                        true,
                        "OTP sent successfully.",
                        user.Email);
                },
                cancellationToken);
        }
    }
}

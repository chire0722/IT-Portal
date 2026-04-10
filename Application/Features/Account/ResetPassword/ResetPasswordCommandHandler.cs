using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Account.ResetPassword
{
    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailBackgroundQueue _emailBackgroundQueue;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;

        public ResetPasswordCommandHandler(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IEmailBackgroundQueue emailBackgroundQueue,
            ILogger<ResetPasswordCommandHandler> logger)
        {
            _userManager = userManager;
            _context = context;
            _emailBackgroundQueue = emailBackgroundQueue;
            _logger = logger;
        }

        public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new ResetPasswordResult(
                    false,
                    true,
                    Array.Empty<string>());
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return new ResetPasswordResult(
                    false,
                    false,
                    result.Errors.Select(e => e.Description).ToList());
            }

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
                Action = "Reset Password (OTP)",
                PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
                EntityName = "User Account",
                EntityId = user.Id,
                Details = "User reset their password via OTP verification.",
                IsSuccess = true
            });
            await QueueSecurityAlertAsync(user, "Password Reset (OTP)", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _userManager.UpdateSecurityStampAsync(user);

            return new ResetPasswordResult(
                true,
                false,
                Array.Empty<string>());
        }

        private async Task QueueSecurityAlertAsync(ApplicationUser user, string method, CancellationToken cancellationToken)
        {
            string subject = "IT Form - Security Alert: Password Changed";
            string body = $@"
                <div style='font-family: sans-serif; padding: 20px; border: 1px solid #f87171; border-radius: 8px; background: #fffafb;'>
                    <h2 style='color: #dc2626;'>Security Alert</h2>
                    <p>Hello <b>{user.FullName}</b>,</p>
                    <p>This is an automated notification to inform you that your password for the IT Asset Management System was recently updated via <b>{method}</b>.</p>
                    <p><b>Time:</b> {DateTime.UtcNow.AddHours(8):dd/MM/yyyy HH:mm:ss} (SGT)</p>
                </div>";

            await _emailBackgroundQueue.QueueEmailAsync(
                new RenderedEmailMessage(user.Email!, subject, body),
                cancellationToken);
            _logger.LogInformation("Queued security alert for {Email} regarding password change.", user.Email);
        }
    }
}

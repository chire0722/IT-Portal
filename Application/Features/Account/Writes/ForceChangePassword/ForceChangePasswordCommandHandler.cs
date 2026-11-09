// Project comment: ForceChangePasswordCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Abstractions;
using FluentValidation;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

/// <summary>
/// Handles the ForceChangePasswordCommand command workflow.
/// </summary>
public sealed class ForceChangePasswordCommandHandler : IRequestHandler<ForceChangePasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailBackgroundQueue _emailBackgroundQueue;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ForceChangePasswordCommandHandler> _logger;

    public ForceChangePasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailBackgroundQueue emailBackgroundQueue,
        IMemoryCache cache,
        ILogger<ForceChangePasswordCommandHandler> logger,
        ICurrentUserAccessor currentUserAccessor)
    {
        _userManager = userManager;
        _context = context;
        _emailBackgroundQueue = emailBackgroundQueue;
        _cache = cache;
        _logger = logger;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(ForceChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _userManager.GetUserId(_currentUserAccessor.User);
        if (string.IsNullOrWhiteSpace(currentUserId) ||
            !string.Equals(currentUserId, request.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You can only change your own password.");
        }

        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), currentUserId);
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Inactive accounts cannot change passwords.");
        }

        if (!user.MustChangePassword && !string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new ValidationException("This forced password-change flow is not available for this account. Use the profile password-change process.");
        }

        var result = string.IsNullOrWhiteSpace(user.PasswordHash)
            ? await _userManager.AddPasswordAsync(user, request.NewPassword)
            : await _userManager.ResetPasswordAsync(
                user,
                await _userManager.GeneratePasswordResetTokenAsync(user),
                request.NewPassword);
        result.EnsureSucceeded();

        user.MustChangePassword = false;
        (await _userManager.UpdateAsync(user)).EnsureSucceeded();
        _cache.Remove($"claims-transform:{user.Id}");

        var auditTime = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            CreatedAt = auditTime,
            Timestamp = auditTime,
            Action = "Change Password",
            PerformedBy = user.FullName ?? user.UserName ?? "Unknown",
            EntityName = "User Account",
            EntityId = user.Id,
            Details = "User successfully changed their password.",
            IsSuccess = true
        });
        await QueueSecurityAlertAsync(user, "Forced Password Change", cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        (await _userManager.UpdateSecurityStampAsync(user)).EnsureSucceeded();

        _logger.LogInformation("User {Username} successfully completed forced password change.", user.UserName);
        return Unit.Value;
    }

    /// <summary>
    /// Runs the QueueSecurityAlertAsync operation.
    /// </summary>
    private async Task QueueSecurityAlertAsync(ApplicationUser user, string method, CancellationToken cancellationToken)
    {
        const string subject = "IT Form - Security Alert: Password Changed";
        string body = $@"
                <div style='font-family: sans-serif; padding: 20px; border: 1px solid #f87171; border-radius: 8px; background: #fffafb;'>
                    <h2 style='color: #dc2626;'>Security Alert</h2>
                    <p>Hello <b>{user.FullName}</b>,</p>
                    <p>This is an automated notification to inform you that your password for the IT Asset Management System was recently updated via <b>{method}</b>.</p>
                    <p><b>Time:</b> {DateTime.UtcNow.AddHours(8):dd/MM/yyyy HH:mm:ss} (SGT)</p>
                    <div style='background: #fee2e2; padding: 15px; border-radius: 4px; border: 1px solid #fecaca; margin: 20px 0;'>
                        <p style='margin: 0; color: #991b1b; font-weight: bold;'>If you did not make this change, please contact the IT Department immediately to secure your account.</p>
                    </div>
                    <hr style='border: 0; border-top: 1px solid #fecaca; margin: 20px 0;' />
                    <small style='color: #666;'>YTC IT Department - Automated Security System</small>
                     </div>";

        await _emailBackgroundQueue.QueueEmailAsync(
            new RenderedEmailMessage(user.Email!, subject, body),
            cancellationToken);
    }
}

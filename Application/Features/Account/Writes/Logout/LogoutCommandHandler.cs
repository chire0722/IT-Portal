// Project comment: LogoutCommandHandler.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Security;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.Account.Writes.Logout;

/// <summary>
/// Handles the LogoutCommand command workflow.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        SignInManager<ApplicationUser> signInManager,
        ICurrentUserAccessor currentUserAccessor,
        ApplicationDbContext context,
        ILogger<LogoutCommandHandler> logger)
    {
        _signInManager = signInManager;
        _currentUserAccessor = currentUserAccessor;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = _currentUserAccessor.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var username = user.Identity?.Name ?? "Unknown User";

        var auditTime = DateTime.UtcNow;

        if (!PortalTitles.IsSuperAdmin(user))
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                CreatedAt = auditTime,
                Timestamp = auditTime,
                Action = "Logout",
                PerformedBy = username,
                EntityName = "User Account",
                EntityId = userId,
                Details = "User logged out.",
                IsSuccess = true
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        // TODO: 可在此处添加登出审计日志或清理缓存
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return Unit.Value;
    }
}

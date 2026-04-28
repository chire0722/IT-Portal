// Project comment: ToggleUserManagementActiveCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;

/// <summary>
/// Handles the ToggleUserManagementActiveCommand command workflow.
/// </summary>
public sealed class ToggleUserManagementActiveCommandHandler : IRequestHandler<ToggleUserManagementActiveCommand, ToggleUserManagementActiveResult>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<ToggleUserManagementActiveCommandHandler> _logger;

    public ToggleUserManagementActiveCommandHandler(
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<ToggleUserManagementActiveCommandHandler> logger)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ToggleUserManagementActiveResult> Handle(
        ToggleUserManagementActiveCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserCompanies)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.Id);
        }

        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (!CompanyScopeHelper.AreAllCompaniesAllowed(scopedCompanies, user.UserCompanies.Select(company => company.CompanyName)))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You cannot modify users outside your assigned company scope.")]);
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var status = user.IsActive ? "activated" : "deactivated";
        _logger.LogInformation("IT admin {Status} user {Username}.", status, user.UserName);

        return new ToggleUserManagementActiveResult(user.FullName, user.IsActive);
    }
}

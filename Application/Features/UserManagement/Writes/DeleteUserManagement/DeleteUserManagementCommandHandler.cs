// Project comment: DeleteUserManagementCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Support;
using IT_Form.Helpers;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;

/// <summary>
/// Handles the DeleteUserManagementCommand command workflow.
/// </summary>
public sealed class DeleteUserManagementCommandHandler : IRequestHandler<DeleteUserManagementCommand, string>
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DeleteUserManagementCommandHandler> _logger;

    public DeleteUserManagementCommandHandler(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteUserManagementCommandHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<string> Handle(DeleteUserManagementCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserTitles)
            .Include(u => u.UserCompanies)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(ApplicationUser), request.Id);
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        var currentUser = principal == null ? null : await _userManager.GetUserAsync(principal);
        if (currentUser != null && currentUser.Id == user.Id)
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You cannot delete your own account.")]);
        }

        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(principal);
        if (!CompanyScopeHelper.AreAllCompaniesAllowed(scopedCompanies, user.UserCompanies.Select(company => company.CompanyName)))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You cannot delete users outside your assigned company scope.")]);
        }

        var name = user.FullName;
        _context.UserTitles.RemoveRange(user.UserTitles);
        _context.UserCompanies.RemoveRange(user.UserCompanies);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Username} was deleted by IT admin.", user.UserName);
        return name;
    }
}

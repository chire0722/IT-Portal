using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement;

public sealed class UpdateUserManagementCommandHandler : IRequestHandler<UpdateUserManagementCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<UpdateUserManagementCommandHandler> _logger;

    public UpdateUserManagementCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<UpdateUserManagementCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateUserManagementCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserTitles)
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

        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanies, cancellationToken);

        request.Adapt(user);

        var userUpdateResult = await _userManager.UpdateAsync(user);
        userUpdateResult.EnsureSucceeded();

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResetResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            passwordResetResult.EnsureSucceeded();

            user.MustChangePassword = true;
            (await _userManager.UpdateAsync(user)).EnsureSucceeded();
        }

        _context.UserTitles.RemoveRange(user.UserTitles);
        _context.UserCompanies.RemoveRange(user.UserCompanies);
        await UserManagementRules.AssignTitlesAsync(_context, user.Id, request.IsIT, request.IsHR, request.IsHOD);
        UserManagementRules.AssignCompanies(_context, user.Id, request.SelectedCompanies);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("IT admin updated user {Username}.", user.UserName);
        return Unit.Value;
    }

    private async Task EnsureRequestedCompaniesAreInScopeAsync(
        IEnumerable<string>? selectedCompanies,
        CancellationToken cancellationToken)
    {
        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var requestedCompanies = selectedCompanies?
            .Where(company => !string.IsNullOrWhiteSpace(company))
            .ToList() ?? [];

        if (!CompanyScopeHelper.AreAllCompaniesAllowed(scopedCompanies, requestedCompanies))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign users to your own company scope.")]);
        }

        var knownCompanies = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyDisplayNamesAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (!requestedCompanies.All(company => knownCompanies.Any(knownCompany => CompanyHelper.IsMatch(knownCompany, company))))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "One or more selected companies are not available in your company scope.")]);
        }
    }
}

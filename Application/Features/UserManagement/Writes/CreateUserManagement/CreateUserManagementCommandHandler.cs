using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Validation;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

public sealed class CreateUserManagementCommandHandler : IRequestHandler<CreateUserManagementCommand, string>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly ILogger<CreateUserManagementCommandHandler> _logger;

    public CreateUserManagementCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<CreateUserManagementCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _logger = logger;
    }

    public async Task<string> Handle(CreateUserManagementCommand request, CancellationToken cancellationToken)
    {
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanies, cancellationToken);

        var user = request.Adapt<ApplicationUser>();

        var result = await _userManager.CreateAsync(user, request.Password!);
        result.EnsureSucceeded();

        await UserManagementRules.AssignTitlesAsync(_context, user.Id, request.IsIT, request.IsHR, request.IsHOD);
        UserManagementRules.AssignCompanies(_context, user.Id, request.SelectedCompanies);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("IT admin created user {Username} with titles.", request.Username);
        return user.Id;
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

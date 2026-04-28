// Project comment: UpdateDepartmentCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Departments.Writes.UpdateDepartment;

/// <summary>
/// Handles the UpdateDepartmentCommand command workflow.
/// </summary>
public sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public UpdateDepartmentCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments
            .Include(department => department.DepartmentCompanies)
            .FirstOrDefaultAsync(department => department.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        await EnsureExistingDepartmentIsInScopeAsync(entity.DepartmentCompanies.Select(link => link.CompanyId), cancellationToken);
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        request.Adapt(entity);

        var currentCompanyIds = entity.DepartmentCompanies
            .Select(link => link.CompanyId)
            .ToHashSet();

        var selectedCompanyIds = request.SelectedCompanyIds
            .Distinct()
            .ToHashSet();

        var companyLinksToRemove = entity.DepartmentCompanies
            .Where(link => !selectedCompanyIds.Contains(link.CompanyId))
            .ToList();

        if (companyLinksToRemove.Count > 0)
        {
            _context.DepartmentCompanies.RemoveRange(companyLinksToRemove);
        }

        var companyLinksToAdd = selectedCompanyIds
            .Except(currentCompanyIds)
            .Select(companyId => new DepartmentCompany
            {
                DepartmentId = entity.Id,
                CompanyId = companyId
            })
            .ToList();

        if (companyLinksToAdd.Count > 0)
        {
            await _context.DepartmentCompanies.AddRangeAsync(companyLinksToAdd, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    /// <summary>
    /// Runs the EnsureExistingDepartmentIsInScopeAsync operation.
    /// </summary>
    private async Task EnsureExistingDepartmentIsInScopeAsync(IEnumerable<int> existingCompanyIds, CancellationToken cancellationToken)
    {
        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (accessibleCompanyIds is null || !existingCompanyIds.All(accessibleCompanyIds.Contains))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only modify departments within your own company scope.")]);
        }
    }

    /// <summary>
    /// Runs the EnsureRequestedCompaniesAreInScopeAsync operation.
    /// </summary>
    private async Task EnsureRequestedCompaniesAreInScopeAsync(IEnumerable<int> selectedCompanyIds, CancellationToken cancellationToken)
    {
        var isAssignable = await ItAdminCompanyScopeSupport.AreRequestedCompanyIdsAssignableAsync(
            _context,
            _currentUserAccessor.User,
            selectedCompanyIds,
            cancellationToken);

        if (!isAssignable)
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign departments to your own company scope.")]);
        }
    }
}

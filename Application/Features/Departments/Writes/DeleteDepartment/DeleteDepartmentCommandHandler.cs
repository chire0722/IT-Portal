using IT_Form.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Departments.Writes.DeleteDepartment;

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DeleteDepartmentCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<Unit> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments
            .Include(department => department.DepartmentCompanies)
            .FirstOrDefaultAsync(department => department.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        await EnsureExistingDepartmentIsInScopeAsync(entity.DepartmentCompanies.Select(link => link.CompanyId), cancellationToken);

        if (entity.DepartmentCompanies.Count > 0)
        {
            _context.DepartmentCompanies.RemoveRange(entity.DepartmentCompanies);
        }

        _context.Departments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureExistingDepartmentIsInScopeAsync(IEnumerable<int> existingCompanyIds, CancellationToken cancellationToken)
    {
        var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var accessibleCompanyIds = await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (!existingCompanyIds.All(accessibleCompanyIds.Contains))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only delete departments within your own company scope.")]);
        }
    }
}

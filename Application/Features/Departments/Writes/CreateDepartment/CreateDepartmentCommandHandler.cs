// Project comment: CreateDepartmentCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;
using MediatR;
using DepartmentEntity = IT_Form.Models.Department;

namespace IT_Form.Application.Features.Departments.Writes.CreateDepartment;

/// <summary>
/// Handles the CreateDepartmentCommand command workflow.
/// </summary>
public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IQuotaControl _quotaControl;

    public CreateDepartmentCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, IQuotaControl? quotaControl = null)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _quotaControl = quotaControl ?? new QuotaControl(context);
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        await _quotaControl.EnsureAvailableAsync(QuotaControl.Departments, user: _currentUserAccessor.User, cancellationToken: cancellationToken);
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        var entity = request.Adapt<DepartmentEntity>();

        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
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

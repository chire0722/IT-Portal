// Project comment: CreateEmailGroupCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using EmailGroupEntity = IT_Form.Models.EmailGroup;

namespace IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup;

/// <summary>
/// Handles the CreateEmailGroupCommand command workflow.
/// </summary>
public sealed class CreateEmailGroupCommandHandler : IRequestHandler<CreateEmailGroupCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IQuotaControl _quotaControl;

    public CreateEmailGroupCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor, IQuotaControl? quotaControl = null)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
        _quotaControl = quotaControl ?? new QuotaControl(context);
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateEmailGroupCommand request, CancellationToken cancellationToken)
    {
        await _quotaControl.EnsureAvailableAsync(QuotaControl.EmailGroups, user: _currentUserAccessor.User, cancellationToken: cancellationToken);
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        var entity = request.Adapt<EmailGroupEntity>();

        _context.EmailGroups.Add(entity);
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
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign email groups to your own company scope.")]);
        }
    }
}

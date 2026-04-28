// Project comment: CreateSharedMailboxCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using SharedMailboxEntity = IT_Form.Models.SharedMailbox;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox;

/// <summary>
/// Handles the CreateSharedMailboxCommand command workflow.
/// </summary>
public sealed class CreateSharedMailboxCommandHandler : IRequestHandler<CreateSharedMailboxCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CreateSharedMailboxCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateSharedMailboxCommand request, CancellationToken cancellationToken)
    {
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        var entity = request.Adapt<SharedMailboxEntity>();

        _context.SharedMailboxes.Add(entity);
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
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign shared mailboxes to your own company scope.")]);
        }
    }
}

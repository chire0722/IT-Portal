using IT_Form.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.DeleteSharedMailbox;

public sealed class DeleteSharedMailboxCommandHandler : IRequestHandler<DeleteSharedMailboxCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DeleteSharedMailboxCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<Unit> Handle(DeleteSharedMailboxCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SharedMailboxes
            .Include(mailbox => mailbox.SharedMailboxCompanies)
            .FirstOrDefaultAsync(mailbox => mailbox.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SharedMailbox), request.Id);

        await EnsureExistingSharedMailboxIsInScopeAsync(entity.SharedMailboxCompanies.Select(link => link.CompanyId), cancellationToken);

        if (entity.SharedMailboxCompanies.Count > 0)
        {
            _context.SharedMailboxCompanies.RemoveRange(entity.SharedMailboxCompanies);
        }

        _context.SharedMailboxes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureExistingSharedMailboxIsInScopeAsync(IEnumerable<int> existingCompanyIds, CancellationToken cancellationToken)
    {
        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (accessibleCompanyIds is null || !existingCompanyIds.All(accessibleCompanyIds.Contains))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only delete shared mailboxes within your own company scope.")]);
        }
    }
}

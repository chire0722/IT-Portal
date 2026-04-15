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

namespace IT_Form.Application.Features.EmailGroups.Writes.DeleteEmailGroup;

public sealed class DeleteEmailGroupCommandHandler : IRequestHandler<DeleteEmailGroupCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DeleteEmailGroupCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<Unit> Handle(DeleteEmailGroupCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EmailGroups
            .Include(group => group.EmailGroupCompanies)
            .FirstOrDefaultAsync(group => group.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailGroup), request.Id);

        await EnsureExistingEmailGroupIsInScopeAsync(entity.EmailGroupCompanies.Select(link => link.CompanyId), cancellationToken);

        if (entity.EmailGroupCompanies.Count > 0)
        {
            _context.EmailGroupCompanies.RemoveRange(entity.EmailGroupCompanies);
        }

        _context.EmailGroups.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureExistingEmailGroupIsInScopeAsync(IEnumerable<int> existingCompanyIds, CancellationToken cancellationToken)
    {
        var scopedCompanies = ItAdminCompanyScopeSupport.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var accessibleCompanyIds = await ItAdminCompanyScopeSupport.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (accessibleCompanyIds is null || !existingCompanyIds.All(accessibleCompanyIds.Contains))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only delete email groups within your own company scope.")]);
        }
    }
}

using IT_Form.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder;

public sealed class UpdateServerFolderCommandHandler : IRequestHandler<UpdateServerFolderCommand, Unit>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public UpdateServerFolderCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<Unit> Handle(UpdateServerFolderCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ServerFolders
            .Include(folder => folder.ServerFolderCompanies)
            .FirstOrDefaultAsync(folder => folder.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServerFolder), request.Id);

        await EnsureExistingServerFolderIsInScopeAsync(entity.ServerFolderCompanies.Select(link => link.CompanyId), cancellationToken);
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        request.Adapt(entity);

        var currentCompanyIds = entity.ServerFolderCompanies
            .Select(link => link.CompanyId)
            .ToHashSet();

        var selectedCompanyIds = request.SelectedCompanyIds
            .Distinct()
            .ToHashSet();

        var companyLinksToRemove = entity.ServerFolderCompanies
            .Where(link => !selectedCompanyIds.Contains(link.CompanyId))
            .ToList();

        if (companyLinksToRemove.Count > 0)
        {
            _context.ServerFolderCompanies.RemoveRange(companyLinksToRemove);
        }

        var companyLinksToAdd = selectedCompanyIds
            .Except(currentCompanyIds)
            .Select(companyId => new ServerFolderCompany
            {
                ServerFolderId = entity.Id,
                CompanyId = companyId
            })
            .ToList();

        if (companyLinksToAdd.Count > 0)
        {
            await _context.ServerFolderCompanies.AddRangeAsync(companyLinksToAdd, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async Task EnsureExistingServerFolderIsInScopeAsync(IEnumerable<int> existingCompanyIds, CancellationToken cancellationToken)
    {
        var scopedCompanies = CompanyScopeHelper.GetScopedCompanies(_currentUserAccessor.User);
        if (scopedCompanies.Count == 0)
        {
            return;
        }

        var accessibleCompanyIds = await CompanyScopeHelper.LoadAccessibleCompanyIdsAsync(_context, _currentUserAccessor.User, cancellationToken);
        if (!existingCompanyIds.All(accessibleCompanyIds.Contains))
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only modify server folders within your own company scope.")]);
        }
    }

    private async Task EnsureRequestedCompaniesAreInScopeAsync(IEnumerable<int> selectedCompanyIds, CancellationToken cancellationToken)
    {
        var isAssignable = await CompanyScopeHelper.AreRequestedCompanyIdsAssignableAsync(
            _context,
            _currentUserAccessor.User,
            selectedCompanyIds,
            cancellationToken);

        if (!isAssignable)
        {
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign server folders to your own company scope.")]);
        }
    }
}

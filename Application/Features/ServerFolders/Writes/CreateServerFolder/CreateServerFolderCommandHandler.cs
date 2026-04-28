// Project comment: CreateServerFolderCommandHandler.cs is part of the IT Form application source code.

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using ServerFolderEntity = IT_Form.Models.ServerFolder;

namespace IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;

/// <summary>
/// Handles the CreateServerFolderCommand command workflow.
/// </summary>
public sealed class CreateServerFolderCommandHandler : IRequestHandler<CreateServerFolderCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CreateServerFolderCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateServerFolderCommand request, CancellationToken cancellationToken)
    {
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        var entity = request.Adapt<ServerFolderEntity>();

        _context.ServerFolders.Add(entity);
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
            throw new ValidationException([new ValidationFailure(string.Empty, "You can only assign server folders to your own company scope.")]);
        }
    }
}

using FluentValidation;
using FluentValidation.Results;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Data;
using IT_Form.Helpers;
using Mapster;
using MediatR;
using ServerFolderEntity = IT_Form.Models.ServerFolder;

namespace IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;

public sealed class CreateServerFolderCommandHandler : IRequestHandler<CreateServerFolderCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CreateServerFolderCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
    {
        _context = context;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<int> Handle(CreateServerFolderCommand request, CancellationToken cancellationToken)
    {
        await EnsureRequestedCompaniesAreInScopeAsync(request.SelectedCompanyIds, cancellationToken);

        var entity = request.Adapt<ServerFolderEntity>();

        _context.ServerFolders.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
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

using MediatR;

namespace IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder;

public sealed record UpdateServerFolderCommand(
    int Id,
    string FolderName,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

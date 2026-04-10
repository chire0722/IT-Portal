using MediatR;

namespace IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;

public sealed record CreateServerFolderCommand(
    string FolderName,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

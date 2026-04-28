// Project comment: UpdateServerFolderCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder;

/// <summary>
/// Carries data needed to execute the UpdateServerFolder command.
/// </summary>
public sealed record UpdateServerFolderCommand(
    int Id,
    string FolderName,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

// Project comment: CreateServerFolderCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;

/// <summary>
/// Carries data needed to execute the CreateServerFolder command.
/// </summary>
public sealed record CreateServerFolderCommand(
    string FolderName,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

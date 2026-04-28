// Project comment: AddCurrentEmployeeToServerFolderCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    /// <summary>
    /// Carries data needed to execute the AddCurrentEmployeeToServerFolder command.
    /// </summary>
    public sealed record AddCurrentEmployeeToServerFolderCommand(
        int ServerFolderId,
        int OnboardingRequestId,
        bool CanRead,
        bool CanModify) : IRequest<AddCurrentEmployeeToServerFolderResult>;
}

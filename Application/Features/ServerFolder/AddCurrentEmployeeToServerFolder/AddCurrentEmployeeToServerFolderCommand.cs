using MediatR;

namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    public sealed record AddCurrentEmployeeToServerFolderCommand(
        int ServerFolderId,
        int OnboardingRequestId,
        bool CanRead,
        bool CanModify) : IRequest<AddCurrentEmployeeToServerFolderResult>;
}

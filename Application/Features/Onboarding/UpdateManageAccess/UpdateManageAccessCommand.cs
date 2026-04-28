// Project comment: UpdateManageAccessCommand.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    /// <summary>
    /// Carries data needed to execute the UpdateManageAccess command.
    /// </summary>
    public sealed record UpdateManageAccessCommand(
        int OnboardingId,
        IReadOnlyCollection<int>? SelectedEmailGroupIds,
        string? OtherGroupDetails,
        IReadOnlyCollection<int>? SelectedSharedMailboxIds,
        string? OtherSharedMailboxDetails,
        IReadOnlyCollection<FolderPermissionViewModel>? FolderPermissions,
        string? OtherFolderDetails) : IRequest<UpdateManageAccessResult>;
}

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    public sealed record UpdateManageAccessCommand(
        int OnboardingId,
        IReadOnlyCollection<int>? SelectedEmailGroupIds,
        string? OtherGroupDetails,
        IReadOnlyCollection<int>? SelectedSharedMailboxIds,
        string? OtherSharedMailboxDetails,
        IReadOnlyCollection<FolderPermissionViewModel>? FolderPermissions,
        string? OtherFolderDetails) : IRequest<UpdateManageAccessResult>;
}

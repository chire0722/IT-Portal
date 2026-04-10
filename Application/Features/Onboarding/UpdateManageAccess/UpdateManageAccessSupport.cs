using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    internal static class UpdateManageAccessSupport
    {
        internal static AccessSelections BuildSelections(
            AccessCatalogDto accessCatalog,
            UpdateManageAccessCommand request,
            int onboardingRequestId)
        {
            var allowedEmailGroups = accessCatalog.EmailGroups.ToDictionary(g => g.Id);
            var allowedSharedMailboxes = accessCatalog.SharedMailboxes.ToDictionary(m => m.Id);
            var allowedFolderNames = accessCatalog.ServerFolders
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var replacementEmailGroups = (request.SelectedEmailGroupIds ?? Array.Empty<int>())
                .Distinct()
                .Where(allowedEmailGroups.ContainsKey)
                .Select(id =>
                {
                    var emailGroup = allowedEmailGroups[id];
                    return new RequestEmailGroup
                    {
                        OnboardingRequestId = onboardingRequestId,
                        EmailGroupId = emailGroup.Id,
                        GroupName = emailGroup.GroupName,
                        GroupEmail = emailGroup.GroupEmail
                    };
                })
                .ToList();

            var replacementSharedMailboxes = (request.SelectedSharedMailboxIds ?? Array.Empty<int>())
                .Distinct()
                .Where(allowedSharedMailboxes.ContainsKey)
                .Select(id =>
                {
                    var mailbox = allowedSharedMailboxes[id];
                    return new RequestSharedMailbox
                    {
                        OnboardingRequestId = onboardingRequestId,
                        SharedMailboxId = mailbox.Id,
                        MailboxName = mailbox.MailboxName,
                        MailboxAddress = mailbox.MailboxAddress
                    };
                })
                .ToList();

            var replacementFolderPermissions = (request.FolderPermissions ?? Array.Empty<FolderPermissionViewModel>())
                .Where(fp => !string.IsNullOrWhiteSpace(fp.FolderName))
                .Where(fp => allowedFolderNames.Contains(fp.FolderName))
                .Where(fp => fp.CanRead || fp.CanModify)
                .Select(permission => new FolderPermission
                {
                    OnboardingRequestId = onboardingRequestId,
                    FolderName = permission.FolderName.Trim(),
                    CanRead = permission.CanRead,
                    CanModify = permission.CanModify
                })
                .ToList();

            return new AccessSelections(
                replacementEmailGroups,
                replacementSharedMailboxes,
                replacementFolderPermissions);
        }

        internal static void ReplaceAccessCollections(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            AccessSelections selections)
        {
            if (onboardingRequest.RequestEmailGroups.Any())
            {
                context.RequestEmailGroups.RemoveRange(onboardingRequest.RequestEmailGroups);
            }

            if (onboardingRequest.RequestSharedMailboxes.Any())
            {
                context.RequestSharedMailboxes.RemoveRange(onboardingRequest.RequestSharedMailboxes);
            }

            if (onboardingRequest.FolderPermissions.Any())
            {
                context.FolderPermissions.RemoveRange(onboardingRequest.FolderPermissions);
            }

            onboardingRequest.RequestEmailGroups = selections.EmailGroups;
            onboardingRequest.RequestSharedMailboxes = selections.SharedMailboxes;
            onboardingRequest.FolderPermissions = selections.FolderPermissions;

            if (selections.EmailGroups.Any())
            {
                context.RequestEmailGroups.AddRange(selections.EmailGroups);
            }

            if (selections.SharedMailboxes.Any())
            {
                context.RequestSharedMailboxes.AddRange(selections.SharedMailboxes);
            }

            if (selections.FolderPermissions.Any())
            {
                context.FolderPermissions.AddRange(selections.FolderPermissions);
            }
        }

        internal static void ApplyOtherAccessFields(OnboardingRequest onboardingRequest, UpdateManageAccessCommand request)
        {
            onboardingRequest.OtherGroup = !string.IsNullOrWhiteSpace(request.OtherGroupDetails);
            onboardingRequest.OtherGroupDetails = string.IsNullOrWhiteSpace(request.OtherGroupDetails)
                ? null
                : request.OtherGroupDetails.Trim();

            onboardingRequest.OtherSharedMailboxDetails = string.IsNullOrWhiteSpace(request.OtherSharedMailboxDetails)
                ? null
                : request.OtherSharedMailboxDetails.Trim();
            onboardingRequest.HasOtherSharedMailbox = !string.IsNullOrWhiteSpace(onboardingRequest.OtherSharedMailboxDetails);

            onboardingRequest.OtherFolderDetails = string.IsNullOrWhiteSpace(request.OtherFolderDetails)
                ? null
                : request.OtherFolderDetails.Trim();
            onboardingRequest.HasOtherFolder = !string.IsNullOrWhiteSpace(onboardingRequest.OtherFolderDetails);
        }
    }

    internal sealed record AccessSelections(
        List<RequestEmailGroup> EmailGroups,
        List<RequestSharedMailbox> SharedMailboxes,
        List<FolderPermission> FolderPermissions);
}

// Project comment: EditOnboardingRequestSupport.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.EditOnboardingRequest
{
    /// <summary>
    /// Defines the EditOnboardingRequestSupport class used by the IT Form application.
    /// </summary>
    internal static class EditOnboardingRequestSupport
    {
        /// <summary>
        /// Applies changes for the ApplyCoreFields operation.
        /// </summary>
        internal static void ApplyCoreFields(OnboardingRequest target, OnboardingRequest source)
        {
            target.FullName = source.FullName;
            target.EmployeeID = source.EmployeeID;
            target.Designation = source.Designation;
            target.Department = source.Department;
            target.Branch = source.Branch;
            target.DeskNumber = source.DeskNumber;
            target.JoinDate = source.JoinDate;
            target.EmailAddress = source.EmailAddress;
            target.HODEmail = source.HODEmail;
            target.OtherGroup = source.OtherGroup;
            target.OtherGroupDetails = source.OtherGroupDetails;
            target.MailboxAccount = source.MailboxAccount;
            target.MailboxLogistic = source.MailboxLogistic;
            target.MailboxInfo = source.MailboxInfo;
            target.MailboxAdmin = source.MailboxAdmin;
            target.MailboxIntern = source.MailboxIntern;
            target.MailboxIT = source.MailboxIT;
            target.MailboxSupervisor = source.MailboxSupervisor;
            target.MailboxStore = source.MailboxStore;
            target.PrecastGroup = source.PrecastGroup;
            target.HasOtherSharedMailbox = source.HasOtherSharedMailbox;
            target.OtherSharedMailboxDetails = source.OtherSharedMailboxDetails;
            target.HasOtherFolder = source.HasOtherFolder;
            target.OtherFolderDetails = source.OtherFolderDetails;
        }

        /// <summary>
        /// Runs the ReplaceRequestedSoftwareAsync operation.
        /// </summary>
        internal static async Task ReplaceRequestedSoftwareAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            IReadOnlyCollection<int> selectedStandardSoftwareIds,
            CancellationToken cancellationToken)
        {
            context.RequestSoftware.RemoveRange(onboardingRequest.RequestSoftware);
            onboardingRequest.RequestSoftware = new List<RequestSoftware>();

            foreach (var softwareId in selectedStandardSoftwareIds)
            {
                var software = await context.StandardSoftware.FindAsync(new object[] { softwareId }, cancellationToken);
                if (software == null)
                {
                    continue;
                }

                onboardingRequest.RequestSoftware.Add(new RequestSoftware
                {
                    StandardSoftwareId = softwareId,
                    SoftwareName = software.SoftwareName
                });
            }
        }

        /// <summary>
        /// Runs the ReplaceEmailGroupsAsync operation.
        /// </summary>
        internal static async Task ReplaceEmailGroupsAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            IReadOnlyCollection<int> selectedEmailGroupIds,
            CancellationToken cancellationToken)
        {
            context.RequestEmailGroups.RemoveRange(onboardingRequest.RequestEmailGroups);
            onboardingRequest.RequestEmailGroups = new List<RequestEmailGroup>();

            foreach (var groupId in selectedEmailGroupIds)
            {
                var group = await context.EmailGroups.FindAsync(new object[] { groupId }, cancellationToken);
                if (group == null)
                {
                    continue;
                }

                onboardingRequest.RequestEmailGroups.Add(new RequestEmailGroup
                {
                    EmailGroupId = groupId,
                    GroupName = group.GroupName
                });
            }
        }

        /// <summary>
        /// Runs the ReplaceSharedMailboxesAsync operation.
        /// </summary>
        internal static async Task ReplaceSharedMailboxesAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            IReadOnlyCollection<int> selectedSharedMailboxIds,
            CancellationToken cancellationToken)
        {
            context.RequestSharedMailboxes.RemoveRange(onboardingRequest.RequestSharedMailboxes);
            onboardingRequest.RequestSharedMailboxes = new List<RequestSharedMailbox>();

            foreach (var mailboxId in selectedSharedMailboxIds)
            {
                var mailbox = await context.SharedMailboxes.FindAsync(new object[] { mailboxId }, cancellationToken);
                if (mailbox == null)
                {
                    continue;
                }

                onboardingRequest.RequestSharedMailboxes.Add(new RequestSharedMailbox
                {
                    SharedMailboxId = mailboxId,
                    MailboxName = mailbox.MailboxName
                });
            }
        }

        /// <summary>
        /// Runs the ReplaceIssuedAssets operation.
        /// </summary>
        internal static void ReplaceIssuedAssets(OnboardingRequest onboardingRequest, IReadOnlyCollection<IssuedAsset>? issuedAssets)
        {
            onboardingRequest.IssuedAssets = issuedAssets?
                .Where(a => AssetCategoryHelper.HasSpecifiedCategory(a.AssetType, a.OtherAssetType, a.Description))
                .Select(asset => new IssuedAsset
                {
                    AssetType = asset.AssetType,
                    OtherAssetType = asset.OtherAssetType,
                    Description = asset.Description,
                    SerialNumber = asset.SerialNumber,
                    AssetTag = asset.AssetTag,
                    Qty = asset.Qty,
                    BrandModel = asset.BrandModel
                })
                .ToList() ?? [];
        }

        /// <summary>
        /// Runs the ReplaceIssuedAssets operation.
        /// </summary>
        internal static void ReplaceIssuedAssets(OnboardingRequest onboardingRequest, IReadOnlyCollection<IT_Form.ViewModels.AssetItemViewModel>? issuedAssets)
        {
            onboardingRequest.IssuedAssets = issuedAssets?
                .Where(a => AssetCategoryHelper.HasSpecifiedCategory(a.AssetType, a.OtherAssetType, a.Description))
                .Select(asset => new IssuedAsset
                {
                    AssetType = asset.AssetType,
                    OtherAssetType = asset.OtherAssetType,
                    Description = asset.Description,
                    SerialNumber = asset.SerialNumber,
                    AssetTag = asset.AssetTag,
                    Qty = asset.Qty,
                    BrandModel = asset.BrandModel
                })
                .ToList() ?? [];
        }

        /// <summary>
        /// Applies changes for the ApplyLegacySoftwareFlags operation.
        /// </summary>
        internal static void ApplyLegacySoftwareFlags(OnboardingRequest target, OnboardingRequest source)
        {
            target.HasAdobe = source.HasAdobe;
            target.HasMSProject = source.HasMSProject;
            target.HasAutoDesk = source.HasAutoDesk;
            target.HasGStarCAD = source.HasGStarCAD;
            target.HasOS = source.HasOS;
            target.HasFortiClientVPN = source.HasFortiClientVPN;
            target.HasSQLAccounting = source.HasSQLAccounting;
            target.HasMSVisio = source.HasMSVisio;
            target.HasAutoCAD = source.HasAutoCAD;
            target.HasSketchUp = source.HasSketchUp;
            target.HasMSOffice = source.HasMSOffice;
            target.HasGoogleChrome = source.HasGoogleChrome;
            target.HasMSTeams = source.HasMSTeams;
            target.HasAntivirus = source.HasAntivirus;
            target.HasDWGTrueView = source.HasDWGTrueView;
            target.HasIFlexiHRMS = source.HasIFlexiHRMS;
            target.HasEShare = source.HasEShare;
            target.HasOtherSoftware = source.HasOtherSoftware;
            target.OtherSoftwareDetails = source.OtherSoftwareDetails;
        }
    }
}

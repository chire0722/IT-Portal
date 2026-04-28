// Project comment: OnboardingItSetupPersistence.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.ViewModels;

namespace IT_Form.Application.Features.Onboarding
{
    /// <summary>
    /// Defines the OnboardingItSetupPersistence class used by the IT Form application.
    /// </summary>
    internal static class OnboardingItSetupPersistence
    {
        /// <summary>
        /// Saves data for the SaveDraftAsync operation.
        /// </summary>
        public static async Task SaveDraftAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            await ReplaceRequestSoftwareAsync(context, onboardingRequest, session, cancellationToken);
            await ReplaceRequestEmailGroupsAsync(context, onboardingRequest, session, cancellationToken);
            await ReplaceRequestSharedMailboxesAsync(context, onboardingRequest, session, cancellationToken);
            ReplaceFolderPermissions(context, onboardingRequest, session);

            onboardingRequest.OtherGroup = session.OtherGroup;
            onboardingRequest.OtherGroupDetails = session.OtherGroupDetails;
            onboardingRequest.OtherSharedMailboxDetails = session.OtherSharedMailboxDetails;
            onboardingRequest.SoftwareRemarks = session.SoftwareRemarks;
            onboardingRequest.FolderRemarks = session.FolderRemarks;
            onboardingRequest.OtherFolderDetails = session.OtherFolderDetails;

            onboardingRequest.CheckPhysicalClean = session.CheckPhysicalClean;
            onboardingRequest.CheckO365Setup = session.CheckO365Setup;
            onboardingRequest.CheckWindowsLogin = session.CheckWindowsLogin;
            onboardingRequest.CheckEmailSetup = session.CheckEmailSetup;
            onboardingRequest.CheckSpecificSoftware = session.CheckSpecificSoftware;
            onboardingRequest.CheckGeneralSoftware = session.CheckGeneralSoftware;
            onboardingRequest.CheckAbcGroup = session.CheckAbcGroup;
            onboardingRequest.CheckEmailMailGroup = session.CheckEmailMailGroup;
            onboardingRequest.CheckDomainJoin = session.CheckDomainJoin;
            onboardingRequest.CheckSharedDrive = session.CheckSharedDrive;
            onboardingRequest.CheckWiFiSetup = session.CheckWiFiSetup;
            onboardingRequest.CheckPrinterSetup = session.CheckPrinterSetup;
            onboardingRequest.CheckScanFolder = session.CheckScanFolder;
            onboardingRequest.CheckVPNSetup = session.CheckVPNSetup;
            onboardingRequest.ChecklistRemarks = session.ChecklistRemarks;

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Runs the ReplaceRequestSoftwareAsync operation.
        /// </summary>
        public static async Task ReplaceRequestSoftwareAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            if (onboardingRequest.RequestSoftware != null && onboardingRequest.RequestSoftware.Any())
            {
                context.RequestSoftware.RemoveRange(onboardingRequest.RequestSoftware);
            }

            onboardingRequest.RequestSoftware = new List<RequestSoftware>();
            foreach (var id in (session.SelectedStandardSoftwareIds ?? new List<int>()).Distinct())
            {
                var software = await context.StandardSoftware.FindAsync(new object[] { id }, cancellationToken);
                if (software != null)
                {
                    onboardingRequest.RequestSoftware.Add(new RequestSoftware
                    {
                        StandardSoftwareId = id,
                        SoftwareName = software.SoftwareName
                    });
                }
            }
        }

        /// <summary>
        /// Runs the ReplaceRequestEmailGroupsAsync operation.
        /// </summary>
        public static async Task ReplaceRequestEmailGroupsAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            if (onboardingRequest.RequestEmailGroups != null && onboardingRequest.RequestEmailGroups.Any())
            {
                context.RequestEmailGroups.RemoveRange(onboardingRequest.RequestEmailGroups);
            }

            onboardingRequest.RequestEmailGroups = new List<RequestEmailGroup>();
            foreach (var id in (session.SelectedEmailGroupIds ?? new List<int>()).Distinct())
            {
                var group = await context.EmailGroups.FindAsync(new object[] { id }, cancellationToken);
                if (group != null)
                {
                    onboardingRequest.RequestEmailGroups.Add(new RequestEmailGroup
                    {
                        EmailGroupId = id,
                        GroupName = group.GroupName,
                        GroupEmail = group.GroupEmail
                    });
                }
            }
        }

        /// <summary>
        /// Runs the ReplaceRequestSharedMailboxesAsync operation.
        /// </summary>
        public static async Task ReplaceRequestSharedMailboxesAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            if (onboardingRequest.RequestSharedMailboxes != null && onboardingRequest.RequestSharedMailboxes.Any())
            {
                context.RequestSharedMailboxes.RemoveRange(onboardingRequest.RequestSharedMailboxes);
            }

            onboardingRequest.RequestSharedMailboxes = new List<RequestSharedMailbox>();
            foreach (var id in (session.SelectedSharedMailboxIds ?? new List<int>()).Distinct())
            {
                var mailbox = await context.SharedMailboxes.FindAsync(new object[] { id }, cancellationToken);
                if (mailbox != null)
                {
                    onboardingRequest.RequestSharedMailboxes.Add(new RequestSharedMailbox
                    {
                        SharedMailboxId = id,
                        MailboxName = mailbox.MailboxName,
                        MailboxAddress = mailbox.MailboxAddress
                    });
                }
            }
        }

        /// <summary>
        /// Runs the ReplaceFolderPermissions operation.
        /// </summary>
        public static void ReplaceFolderPermissions(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session)
        {
            context.FolderPermissions.RemoveRange(onboardingRequest.FolderPermissions);
            onboardingRequest.FolderPermissions = new List<FolderPermission>();

            foreach (var permission in session.FolderPermissions ?? new List<FolderPermissionViewModel>())
            {
                onboardingRequest.FolderPermissions.Add(new FolderPermission
                {
                    FolderName = permission.FolderName,
                    CanRead = permission.CanRead,
                    CanModify = permission.CanModify
                });
            }
        }

        /// <summary>
        /// Applies changes for the ApplySessionDetails operation.
        /// </summary>
        public static void ApplySessionDetails(OnboardingRequest onboardingRequest, OnboardingViewModel session)
        {
            onboardingRequest.OtherGroup = session.OtherGroup;
            onboardingRequest.OtherGroupDetails = session.OtherGroupDetails;
            onboardingRequest.AssetRemarks = session.AssetRemarks;
            onboardingRequest.SoftwareRemarks = session.SoftwareRemarks;
            onboardingRequest.FolderRemarks = session.FolderRemarks;
            onboardingRequest.OtherBranchDetails = session.OtherBranchDetails;
            onboardingRequest.OtherDepartmentDetails = session.OtherDepartmentDetails;
            onboardingRequest.OtherSharedMailboxDetails = session.OtherSharedMailboxDetails;
            onboardingRequest.OtherFolderDetails = session.OtherFolderDetails;
            onboardingRequest.HasAdobe = session.HasAdobe;
            onboardingRequest.HasMSProject = session.HasMSProject;
            onboardingRequest.HasAutoDesk = session.HasAutoDesk;
            onboardingRequest.HasGStarCAD = session.HasGStarCAD;
            onboardingRequest.HasFortiClientVPN = session.HasFortiClientVPN;
            onboardingRequest.HasSQLAccounting = session.HasSQLAccounting;
            onboardingRequest.HasMSVisio = session.HasMSVisio;
            onboardingRequest.HasAutoCAD = session.HasAutoCAD;
            onboardingRequest.HasSketchUp = session.HasSketchUp;
            onboardingRequest.HasMSOffice = session.HasMSOffice;
            onboardingRequest.HasGoogleChrome = session.HasGoogleChrome;
            onboardingRequest.HasMSTeams = session.HasMSTeams;
            onboardingRequest.HasAntivirus = session.HasAntivirus;
            onboardingRequest.HasDWGTrueView = session.HasDWGTrueView;
            onboardingRequest.HasIFlexiHRMS = session.HasIFlexiHRMS;
            onboardingRequest.HasEShare = session.HasEShare;
            onboardingRequest.HasOtherSoftware = session.HasOtherSoftware;
            onboardingRequest.OtherSoftwareDetails = session.OtherSoftwareDetails;
            onboardingRequest.MailboxAccount = session.MailboxAccount;
            onboardingRequest.MailboxLogistic = session.MailboxLogistic;
            onboardingRequest.MailboxInfo = session.MailboxInfo;
            onboardingRequest.MailboxAdmin = session.MailboxAdmin;
            onboardingRequest.MailboxIntern = session.MailboxIntern;
            onboardingRequest.MailboxIT = session.MailboxIT;
            onboardingRequest.MailboxSupervisor = session.MailboxSupervisor;
            onboardingRequest.MailboxStore = session.MailboxStore;
            onboardingRequest.PrecastGroup = session.PrecastGroup;
        }
    }
}

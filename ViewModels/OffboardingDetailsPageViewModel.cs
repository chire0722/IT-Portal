// Project comment: OffboardingDetailsPageViewModel.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;
using IT_Form.Helpers;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OffboardingDetailsPage screen or form.
    /// </summary>
    public sealed class OffboardingDetailsPageViewModel
    {
        public int Id { get; set; }
        public int? OnboardingRequestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeID { get; set; }
        public string Department { get; set; } = string.Empty;
        public string? Branch { get; set; }
        public string? EmailAddress { get; set; }
        public string HODEmail { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime LastDayOfWork { get; set; }
        public Guid AccessToken { get; set; }
        public bool AccountDisabledAD { get; set; }
        public bool EmailForwarded { get; set; }
        public string? ForwardToEmail { get; set; }
        public bool M365LicenseRevoked { get; set; }
        public bool SoftwareLicensesRevoked { get; set; }
        public bool LaptopReturned { get; set; }
        public bool AccessoriesReturned { get; set; }
        public bool IsHardwareDamaged { get; set; }
        public string? HardwareNotes { get; set; }
        public string? HardwareDamageImagePath { get; set; }
        public decimal? MissingItemFines { get; set; }
        public string? FinesRemarks { get; set; }
        public bool ClearedLaptop { get; set; }
        public bool ClearedMobile { get; set; }
        public bool ClearedKeys { get; set; }
        public bool ClearedOtherAssets { get; set; }
        public string? ClearanceRemarks { get; set; }
        public bool CheckPhysicalClean { get; set; }
        public bool CheckO365Deactivate { get; set; }
        public bool CheckWindowsDisable { get; set; }
        public bool CheckEmailSignOut { get; set; }
        public bool CheckSpecificSoftwareDeactivate { get; set; }
        public bool CheckGeneralSoftwareDeactivate { get; set; }
        public bool CheckAbcGroupRemove { get; set; }
        public bool CheckEmailMailGroupRemove { get; set; }
        public bool CheckDomainDisjoin { get; set; }
        public bool CheckSharedDriveRemove { get; set; }
        public bool CheckWiFiDisable { get; set; }
        public bool CheckPrinterRemove { get; set; }
        public bool CheckScanFolderRemove { get; set; }
        public bool CheckVPNDisable { get; set; }
        public string? ChecklistRemarks { get; set; }
        public bool DataBackupDone { get; set; }
        public bool EmailBackupDone { get; set; }
        public bool EmailForwardingDone { get; set; }
        public string? EmailForwardName { get; set; }
        public bool LicenseRemovalDone { get; set; }
        public string? FollowUpRemarks { get; set; }
        public string? ResignationLetterPath { get; set; }
        public string? RejectionReason { get; set; }
        public string? RejectedByUserId { get; set; }
        public string? OtherDepartmentDetails { get; set; }
        public string? OtherBranchDetails { get; set; }
        public string? AssignedHodId { get; set; }
        public string? HRSignature { get; set; }
        public string? HRSignerName { get; set; }
        public DateTime? HRSignedAt { get; set; }
        public string? HODSignature { get; set; }
        public string? HODSignerName { get; set; }
        public DateTime? HODSignedAt { get; set; }
        public string? ITAdminSignature { get; set; }
        public string? ITAdminSignerName { get; set; }
        public DateTime? ITAdminSignedAt { get; set; }
        public string? EmployeeSignature { get; set; }
        public string? EmployeeSignerName { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public string? CurrentUserSignature { get; set; }
        public string? CurrentUserId { get; set; }
        public IReadOnlyCollection<string> MyManagerEmails { get; set; } = [];
        public string ManagerDisplayName { get; set; } = "-";
        public int SlaTargetDays { get; set; }
        public DateTime SlaStartTime { get; set; }
        public DateTime SlaDeadline { get; set; }
        public string? IntegrityError { get; set; }
        public string? CompanyLogoPath { get; set; }
        public string CompanyTimeZoneId { get; set; } = CompanyTimeZoneHelper.DefaultTimeZoneId;
        public OffboardingOnboardingSnapshotViewModel? OnboardingRequest { get; set; }
    }

    /// <summary>
    /// Provides view data for the OffboardingOnboardingSnapshot screen or form.
    /// </summary>
    public sealed class OffboardingOnboardingSnapshotViewModel
    {
        public string? FullName { get; set; }
        public string? EmailAddress { get; set; }
        public bool MailboxAccount { get; set; }
        public bool HasAdobe { get; set; }
        public bool HasMSProject { get; set; }
        public bool HasAutoDesk { get; set; }
        public bool HasGStarCAD { get; set; }
        public bool HasFortiClientVPN { get; set; }
        public bool HasSQLAccounting { get; set; }
        public bool HasMSVisio { get; set; }
        public bool HasAutoCAD { get; set; }
        public bool HasSketchUp { get; set; }
        public bool HasMSOffice { get; set; }
        public bool HasGoogleChrome { get; set; }
        public bool HasMSTeams { get; set; }
        public bool HasAntivirus { get; set; }
        public bool HasDWGTrueView { get; set; }
        public bool HasIFlexiHRMS { get; set; }
        public bool HasEShare { get; set; }
        public bool HasOS { get; set; }
        public bool HasOtherSoftware { get; set; }
        public string? OtherSoftwareDetails { get; set; }
        public bool PrecastGroup { get; set; }
        public bool OtherGroup { get; set; }
        public string? OtherGroupDetails { get; set; }
        public bool MailboxLogistic { get; set; }
        public bool MailboxInfo { get; set; }
        public bool MailboxAdmin { get; set; }
        public bool MailboxIntern { get; set; }
        public bool MailboxIT { get; set; }
        public bool MailboxSupervisor { get; set; }
        public bool MailboxStore { get; set; }
        public bool HasOtherFolder { get; set; }
        public string? OtherFolderDetails { get; set; }
        public string? EmployeeSignature { get; set; }
        public string? EmployeeSignerName { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public IReadOnlyList<RequestAssetReadModel> IssuedAssets { get; set; } = [];
        public IReadOnlyList<RequestIssuedSoftwareReadModel> IssuedSoftware { get; set; } = [];
        public IReadOnlyList<RequestFolderPermissionReadModel> FolderPermissions { get; set; } = [];
    }
}

using IT_Form.Models.Enums;

namespace IT_Form.ViewModels
{
    public sealed class OnboardingDetailsPageViewModel
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? EmployeeID { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? Branch { get; set; }
        public string? DeskNumber { get; set; }
        public DateTime? JoinDate { get; set; }
        public string? EmailAddress { get; set; }
        public string? ContactNumber { get; set; }
        public string? HODEmail { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid AccessToken { get; set; }
        public string? RejectionReason { get; set; }
        public string? RejectedByUserId { get; set; }
        public string? OtherDepartmentDetails { get; set; }
        public string? OtherBranchDetails { get; set; }
        public string? OtherSoftwareDetails { get; set; }
        public string? OtherGroupDetails { get; set; }
        public string? OtherSharedMailboxDetails { get; set; }
        public string? OtherFolderDetails { get; set; }
        public string? FolderRemarks { get; set; }
        public bool HasMSOffice { get; set; }
        public bool HasAdobe { get; set; }
        public bool HasMSProject { get; set; }
        public bool HasMSVisio { get; set; }
        public bool HasAutoDesk { get; set; }
        public bool HasAutoCAD { get; set; }
        public bool HasGStarCAD { get; set; }
        public bool HasSketchUp { get; set; }
        public bool HasSQLAccounting { get; set; }
        public bool HasFortiClientVPN { get; set; }
        public bool HasAntivirus { get; set; }
        public bool HasIFlexiHRMS { get; set; }
        public bool HasGoogleChrome { get; set; }
        public bool HasMSTeams { get; set; }
        public bool HasOS { get; set; }
        public bool HasOtherSoftware { get; set; }
        public bool PrecastGroup { get; set; }
        public bool OtherGroup { get; set; }
        public bool MailboxAccount { get; set; }
        public bool MailboxLogistic { get; set; }
        public bool MailboxInfo { get; set; }
        public bool MailboxAdmin { get; set; }
        public bool MailboxIT { get; set; }
        public bool HasOtherSharedMailbox { get; set; }
        public bool HasOtherFolder { get; set; }
        public bool CheckPhysicalClean { get; set; }
        public bool CheckO365Setup { get; set; }
        public bool CheckWindowsLogin { get; set; }
        public bool CheckEmailSetup { get; set; }
        public bool CheckSpecificSoftware { get; set; }
        public bool CheckGeneralSoftware { get; set; }
        public bool CheckAbcGroup { get; set; }
        public bool CheckEmailMailGroup { get; set; }
        public bool CheckDomainJoin { get; set; }
        public bool CheckSharedDrive { get; set; }
        public bool CheckVPNSetup { get; set; }
        public bool CheckPrinterSetup { get; set; }
        public bool CheckScanFolder { get; set; }
        public string? RequesterSignature { get; set; }
        public string? RequesterSignerName { get; set; }
        public DateTime? RequesterSignedAt { get; set; }
        public string? ITAdminSignature { get; set; }
        public string? ITAdminSignerName { get; set; }
        public DateTime? ITAdminSignedAt { get; set; }
        public string? EmployeeSignature { get; set; }
        public string? EmployeeSignerName { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }
        public string? CurrentUserSignature { get; set; }
        public int SlaTargetDays { get; set; }
        public DateTime? SlaStartTime { get; set; }
        public DateTime? SlaDeadline { get; set; }
        public string? IntegrityError { get; set; }
        public string? CompanyLogoPath { get; set; }
        public IReadOnlyList<RequestAssetReadModel> IssuedAssets { get; set; } = [];
        public IReadOnlyList<RequestIssuedSoftwareReadModel> IssuedSoftware { get; set; } = [];
        public IReadOnlyList<RequestSoftwareReadModel> RequestSoftware { get; set; } = [];
        public IReadOnlyList<RequestEmailGroupReadModel> RequestEmailGroups { get; set; } = [];
        public IReadOnlyList<RequestSharedMailboxReadModel> RequestSharedMailboxes { get; set; } = [];
        public IReadOnlyList<RequestFolderPermissionReadModel> FolderPermissions { get; set; } = [];
    }
}

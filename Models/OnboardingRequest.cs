using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT_Form.Models.Enums;
using IT_Form.Attributes;
using Stateless;

namespace IT_Form.Models
{
    public class OnboardingRequest : ISlaTrackable
    {
        [NotMapped]
        private readonly StateMachine<RequestStatus, OnboardingTrigger> _machine;

        public OnboardingRequest()
        {
            _machine = new StateMachine<RequestStatus, OnboardingTrigger>(() => Status, status => Status = status);
            ConfigureStateMachine();
        }

        public int Id { get; set; }

        // --- Primary Info ---
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public RequestStatus Status { get; set; } = RequestStatus.Draft;

        [NotMapped]
        public OnboardingState State => Status switch
        {
            RequestStatus.Draft => OnboardingState.Draft,
            RequestStatus.PendingITApproval => HasItProgress() ? OnboardingState.ItProvisioning : OnboardingState.HrSubmitted,
            RequestStatus.PendingApproval => OnboardingState.HrSubmitted,
            RequestStatus.PendingHRApproval => OnboardingState.HrSubmitted,
            RequestStatus.PendingHODApproval => OnboardingState.HrSubmitted,
            RequestStatus.PendingEmployeeSign => OnboardingState.Completed,
            RequestStatus.Approved => OnboardingState.Completed,
            RequestStatus.Completed => OnboardingState.Completed,
            RequestStatus.Rejected => OnboardingState.Canceled,
            _ => OnboardingState.Draft
        };

        public string? CreatedByUserId { get; set; }

        // --- Employee Details ---
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        [EncryptColumn]
        public string? EmployeeID { get; set; }

        [StringLength(100)]
        public string? Designation { get; set; }

        [StringLength(50)]
        public string? Department { get; set; }

        [StringLength(250)]
        public string? Branch { get; set; }

        [StringLength(50)]
        public string? DeskNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? JoinDate { get; set; }

        [EmailAddress, StringLength(256)]
        [EncryptColumn]
        public string? EmailAddress { get; set; }

        [StringLength(30)]
        [EncryptColumn]
        public string? ContactNumber { get; set; }

        [StringLength(256)]
        [EncryptColumn]
        public string? HODEmail { get; set; }

        // --- IT Assets (Navigation) ---
        public List<IssuedAsset> IssuedAssets { get; set; } = new();

        // --- Software Assignments (Navigation) ---
        public List<IssuedSoftware> IssuedSoftware { get; set; } = new();

        // --- Dynamic Checklists (Navigation) ---
        public List<RequestSoftware> RequestSoftware { get; set; } = new();

        public List<RequestEmailGroup> RequestEmailGroups { get; set; } = new();

        public bool OtherGroup { get; set; }
        [StringLength(200)]
        public string? OtherGroupDetails { get; set; }

        public List<RequestSharedMailbox> RequestSharedMailboxes { get; set; } = new();

        // --- Software Entitlements (Legacy Compatibility) ---
        public bool HasAdobe { get; set; }
        public bool HasMSProject { get; set; }
        public bool HasAutoDesk { get; set; }
        public bool HasGStarCAD { get; set; }
        public bool HasOS { get; set; }
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
        public bool HasOtherSoftware { get; set; }
        [StringLength(500)]
        public string? OtherSoftwareDetails { get; set; }

        public bool PrecastGroup { get; set; }

        public bool MailboxAccount { get; set; }
        public bool MailboxLogistic { get; set; }
        public bool MailboxInfo { get; set; }
        public bool MailboxAdmin { get; set; }
        public bool MailboxIntern { get; set; }
        public bool MailboxIT { get; set; }
        public bool MailboxSupervisor { get; set; }
        public bool MailboxStore { get; set; }

        // --- Server Folder Permissions (Navigation) ---
        public List<FolderPermission> FolderPermissions { get; set; } = new();

        // --- Digital Signatures (Base64) ---
        public string? RequesterSignature { get; set; }
        public string? RequesterSignatureHmac { get; set; }
        public string? RequesterSignerName { get; set; }
        public DateTime? RequesterSignedAt { get; set; }

        public string? ITAdminSignature { get; set; }
        public string? ITAdminSignatureHmac { get; set; }
        public string? ITAdminSignerName { get; set; }
        public DateTime? ITAdminSignedAt { get; set; }

        public string? EmployeeSignature { get; set; }
        public string? EmployeeSignatureHmac { get; set; }
        public string? EmployeeSignerName { get; set; }
        public DateTime? EmployeeSignedAt { get; set; }

        // --- External Access ---
        public Guid AccessToken { get; set; } = Guid.NewGuid();

        // --- Rejection Info ---
        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        [StringLength(450)]
        public string? RejectedByUserId { get; set; }

        // --- Timestamps & Tracking ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public SlaNotifyStatus SlaNotifyStatus { get; set; } = SlaNotifyStatus.Pending;

        // --- IT Checklist (Onboarding Phase) — filled by IT ---
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
        public bool CheckWiFiSetup { get; set; }
        public bool CheckPrinterSetup { get; set; }
        public bool CheckScanFolder { get; set; }
        public bool CheckVPNSetup { get; set; }
        [StringLength(500)]
        public string? ChecklistRemarks { get; set; }

        // --- Multi-step Remarks (Binding Fixes) ---
        public string? AssetRemarks { get; set; }
        public string? SoftwareRemarks { get; set; }
        public string? FolderRemarks { get; set; }

        // --- Additional "Other" Specifications ---
        [StringLength(200)]
        public string? OtherBranchDetails { get; set; }
        [StringLength(200)]
        public string? OtherDepartmentDetails { get; set; }
        [StringLength(200)]
        public string? OtherSharedMailboxDetails { get; set; }
        public bool HasOtherSharedMailbox { get; set; }
        [StringLength(200)]
        public string? OtherFolderDetails { get; set; }
        public bool HasOtherFolder { get; set; }

        // --- PDPA Compliance ---
        public bool IsPdpaConsented { get; set; }
        public DateTime? PdpaConsentedAt { get; set; }

        public bool CanBeHrSignedOff() => State == OnboardingState.Draft;

        public void SubmitByHr()
        {
            _machine.Fire(OnboardingTrigger.SubmitByHr);
        }

        public void StartItSetup()
        {
            _machine.Fire(OnboardingTrigger.StartItSetup);
        }

        public void FinishOnboarding()
        {
            _machine.Fire(OnboardingTrigger.FinishOnboarding);
        }

        public void Cancel()
        {
            if (Status == RequestStatus.Rejected)
            {
                return;
            }

            _machine.Fire(OnboardingTrigger.Cancel);
        }

        public void RestoreDraft()
        {
            Status = RequestStatus.Draft;
        }

        public void SignOffByHr(string signatureBase64, string signerName, string signatureHmac, DateTime signedAtUtc)
        {
            RequesterSignature = signatureBase64;
            RequesterSignerName = signerName;
            RequesterSignatureHmac = signatureHmac;
            RequesterSignedAt = signedAtUtc;
            SubmitByHr();
            UpdatedAt = signedAtUtc;
        }

        public bool CanBeItSigned() => Status == RequestStatus.PendingITApproval;

        public bool CanBeEdited() =>
            Status != RequestStatus.PendingEmployeeSign &&
            Status != RequestStatus.Completed;

        public bool CanStartItSetup() => Status == RequestStatus.PendingITApproval;

        public bool CanRegenerateToken() => Status == RequestStatus.PendingEmployeeSign;

        public void ApplyItChecklist(
            bool checkPhysicalClean,
            bool checkO365Setup,
            bool checkWindowsLogin,
            bool checkEmailSetup,
            bool checkSpecificSoftware,
            bool checkGeneralSoftware,
            bool checkAbcGroup,
            bool checkEmailMailGroup,
            bool checkDomainJoin,
            bool checkSharedDrive,
            bool checkWiFiSetup,
            bool checkPrinterSetup,
            bool checkScanFolder,
            bool checkVpnSetup,
            string? checklistRemarks,
            DateTime updatedAtUtc)
        {
            CheckPhysicalClean = checkPhysicalClean;
            CheckO365Setup = checkO365Setup;
            CheckWindowsLogin = checkWindowsLogin;
            CheckEmailSetup = checkEmailSetup;
            CheckSpecificSoftware = checkSpecificSoftware;
            CheckGeneralSoftware = checkGeneralSoftware;
            CheckAbcGroup = checkAbcGroup;
            CheckEmailMailGroup = checkEmailMailGroup;
            CheckDomainJoin = checkDomainJoin;
            CheckSharedDrive = checkSharedDrive;
            CheckWiFiSetup = checkWiFiSetup;
            CheckPrinterSetup = checkPrinterSetup;
            CheckScanFolder = checkScanFolder;
            CheckVPNSetup = checkVpnSetup;
            ChecklistRemarks = checklistRemarks;
            UpdatedAt = updatedAtUtc;
        }

        public bool HasCompleteItChecklist() =>
            CheckPhysicalClean &&
            CheckO365Setup &&
            CheckWindowsLogin &&
            CheckEmailSetup &&
            CheckSpecificSoftware &&
            CheckGeneralSoftware &&
            CheckAbcGroup &&
            CheckEmailMailGroup &&
            CheckDomainJoin &&
            CheckSharedDrive &&
            CheckWiFiSetup &&
            CheckPrinterSetup &&
            CheckScanFolder &&
            CheckVPNSetup;

        public void SignByItAdmin(string signatureBase64, string signerName, string signatureHmac, DateTime signedAtUtc)
        {
            ITAdminSignature = signatureBase64;
            ITAdminSignerName = signerName;
            ITAdminSignatureHmac = signatureHmac;
            ITAdminSignedAt = signedAtUtc;
            FinishOnboarding();
            CompletedAt = signedAtUtc;
            UpdatedAt = signedAtUtc;
        }

        public void CompleteItSetup(string? signatureBase64, string signerName, string? signatureHmac, DateTime signedAtUtc)
        {
            ITAdminSignature = signatureBase64;
            ITAdminSignerName = signerName;
            ITAdminSignatureHmac = signatureHmac;
            ITAdminSignedAt = signedAtUtc;
            FinishOnboarding();
            CompletedAt = signedAtUtc;
            UpdatedAt = signedAtUtc;
        }

        public void SubmitDraftByHr(string? signatureBase64, string signerName, string? signatureHmac, DateTime submittedAtUtc)
        {
            RequesterSignature = signatureBase64 ?? RequesterSignature;
            RequesterSignedAt = submittedAtUtc;
            RequesterSignerName = signerName;
            RequesterSignatureHmac = signatureHmac;
            SubmitByHr();
            UpdatedAt = submittedAtUtc;
        }

        public bool CanBeRejected() =>
            Status != RequestStatus.Rejected &&
            _machine.CanFire(OnboardingTrigger.Cancel);

        public void Reject(string reason, string? rejectedByUserId, DateTime updatedAtUtc)
        {
            Cancel();
            RejectionReason = reason;
            RejectedByUserId = rejectedByUserId;
            UpdatedAt = updatedAtUtc;
        }

        public void RegenerateAccessToken(DateTime updatedAtUtc)
        {
            AccessToken = Guid.NewGuid();
            UpdatedAt = updatedAtUtc;
        }

        public bool CanBeDeletedBy(bool isHr)
        {
            var isSignedByHrOnly = !string.IsNullOrEmpty(RequesterSignature)
                && string.IsNullOrEmpty(ITAdminSignature);

            return Status == RequestStatus.Draft
                || Status == RequestStatus.Rejected
                || (isHr && isSignedByHrOnly);
        }

        private void ConfigureStateMachine()
        {
            _machine.Configure(RequestStatus.Draft)
                .Permit(OnboardingTrigger.SubmitByHr, RequestStatus.PendingITApproval)
                .Permit(OnboardingTrigger.Cancel, RequestStatus.Rejected);

            _machine.Configure(RequestStatus.PendingApproval)
                .Permit(OnboardingTrigger.Cancel, RequestStatus.Rejected);

            _machine.Configure(RequestStatus.PendingHRApproval)
                .Permit(OnboardingTrigger.Cancel, RequestStatus.Rejected);

            _machine.Configure(RequestStatus.PendingHODApproval)
                .Permit(OnboardingTrigger.Cancel, RequestStatus.Rejected);

            _machine.Configure(RequestStatus.PendingITApproval)
                .PermitReentry(OnboardingTrigger.StartItSetup)
                .Permit(OnboardingTrigger.FinishOnboarding, RequestStatus.PendingEmployeeSign)
                .Permit(OnboardingTrigger.Cancel, RequestStatus.Rejected);
        }

        private bool HasItProgress()
            => !string.IsNullOrWhiteSpace(ChecklistRemarks) ||
               !string.IsNullOrWhiteSpace(SoftwareRemarks) ||
               !string.IsNullOrWhiteSpace(FolderRemarks) ||
               CheckPhysicalClean ||
               CheckO365Setup ||
               CheckWindowsLogin ||
               CheckEmailSetup ||
               CheckSpecificSoftware ||
               CheckGeneralSoftware ||
               CheckAbcGroup ||
               CheckEmailMailGroup ||
               CheckDomainJoin ||
               CheckSharedDrive ||
               CheckWiFiSetup ||
               CheckPrinterSetup ||
               CheckScanFolder ||
               CheckVPNSetup;
    }
}

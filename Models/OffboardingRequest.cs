// Project comment: OffboardingRequest.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IT_Form.Models.Enums;
using IT_Form.Attributes;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the OffboardingRequest class used by the IT Form application.
    /// </summary>
    public class OffboardingRequest : ISlaTrackable
    {
        public int Id { get; set; }

        // --- Primary Info ---
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public RequestStatus Status { get; set; } = RequestStatus.Draft;
        public string? CreatedByUserId { get; set; }

        // --- Link to Onboarding (Optional) ---
        // If linked, we know exactly what they were given.
        public int? OnboardingRequestId { get; set; }
        public OnboardingRequest? OnboardingRequest { get; set; }

        // --- Employee Details ---
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        [EncryptColumn]
        public string? EmployeeID { get; set; }

        [Required, StringLength(50)]
        public string Department { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Branch { get; set; } // Company Name now in UI

        [StringLength(50)]
        public string? ProjectCode { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime LastDayOfWork { get; set; }

        [EmailAddress, StringLength(256)]
        [EncryptColumn]
        public string? EmailAddress { get; set; }

        [Required, StringLength(256)]
        [EncryptColumn]
        public string HODEmail { get; set; } = string.Empty;

        // --- IT Checklist ---
        public bool AccountDisabledAD { get; set; }
        public bool EmailForwarded { get; set; }
        [StringLength(256)]
        [EncryptColumn]
        public string? ForwardToEmail { get; set; }
        public bool M365LicenseRevoked { get; set; }
        public bool SoftwareLicensesRevoked { get; set; }

        // --- Hardware Return ---
        public bool LaptopReturned { get; set; }
        public bool AccessoriesReturned { get; set; }
        public bool IsHardwareDamaged { get; set; }
        [StringLength(500)]
        public string? HardwareNotes { get; set; }

        [StringLength(500)]
        public string? HardwareDamageImagePath { get; set; }

        [StringLength(500)]
        public string? HardwareReturnImagePath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MissingItemFines { get; set; }

        [StringLength(500)]
        public string? FinesRemarks { get; set; }

        // --- Digital Signatures (Base64) ---
        public string? HRSignature { get; set; }
        public string? HRSignatureHmac { get; set; }
        public string? HRSignerName { get; set; }
        public DateTime? HRSignedAt { get; set; }

        public string? HODSignature { get; set; }
        public string? HODSignatureHmac { get; set; }
        public string? HODSignerName { get; set; }
        public DateTime? HODSignedAt { get; set; }

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

        // --- Documents ---
        [StringLength(500)]
        public string? ResignationLetterPath { get; set; }

        // --- Rejection Info ---
        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        [StringLength(450)]
        public string? RejectedByUserId { get; set; }

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        // --- Timestamps & Tracking ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public SlaNotifyStatus SlaNotifyStatus { get; set; } = SlaNotifyStatus.Pending;

        // --- Clearance Items (Yes/No) ---
        public bool ClearedLaptop { get; set; }
        public bool ClearedMobile { get; set; }
        public bool ClearedKeys { get; set; }
        public bool ClearedOtherAssets { get; set; }
        [StringLength(500)]
        public string? ClearanceRemarks { get; set; }

        // --- IT Checklist (Offboarding Phase) — filled by IT ---
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
        [StringLength(500)]
        public string? ChecklistRemarks { get; set; }

        // --- Follow-up Offboarding Actions ---
        public bool DataBackupDone { get; set; }
        public bool EmailBackupDone { get; set; }
        public bool EmailForwardingDone { get; set; }
        [StringLength(200)]
        public string? EmailForwardName { get; set; }
        public bool LicenseRemovalDone { get; set; }
        [StringLength(500)]
        public string? FollowUpRemarks { get; set; }

        [StringLength(200)]
        public string? OtherDepartmentDetails { get; set; }
        [StringLength(200)]
        public string? OtherBranchDetails { get; set; }

        // --- PDPA Compliance ---
        public bool IsPdpaConsented { get; set; }
        public DateTime? PdpaConsentedAt { get; set; }

        // --- Role-Based Assignment ---
        [StringLength(450)]
        public string? AssignedHodId { get; set; }

        public virtual ApplicationUser? AssignedHod { get; set; }

        /// <summary>
        /// Runs the CanBeCheckerApprovedBy operation.
        /// </summary>
        public bool CanBeCheckerApprovedBy(string? userName) =>
            string.IsNullOrWhiteSpace(CreatedByUserId) ||
            !CreatedByUserId.Equals(userName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Runs the ApproveChecker operation.
        /// </summary>
        public void ApproveChecker(string? approverId, string? fallbackUserName, DateTime updatedAtUtc)
        {
            ApprovedByUserId = !string.IsNullOrWhiteSpace(approverId) ? approverId : fallbackUserName;

            if (string.IsNullOrWhiteSpace(AssignedHodId) && !string.IsNullOrWhiteSpace(approverId))
            {
                AssignedHodId = approverId;
            }

            if (Status == RequestStatus.PendingApproval)
            {
                Status = RequestStatus.PendingHODApproval;
            }

            UpdatedAt = updatedAtUtc;
        }

        /// <summary>
        /// Runs the CanBeSignedByManager operation.
        /// </summary>
        public bool CanBeSignedByManager(bool assignedMatch, bool emailMatch) => assignedMatch || emailMatch;

        /// <summary>
        /// Runs the CanEditItChecklist operation.
        /// </summary>
        public bool CanEditItChecklist() =>
            Status != RequestStatus.PendingEmployeeSign &&
            Status != RequestStatus.Completed;

        /// <summary>
        /// Returns whether every required IT offboarding control has been completed.
        /// </summary>
        public bool HasCompleteItChecklist() =>
            CheckPhysicalClean &&
            CheckO365Deactivate &&
            CheckWindowsDisable &&
            CheckEmailSignOut &&
            CheckSpecificSoftwareDeactivate &&
            CheckGeneralSoftwareDeactivate &&
            CheckAbcGroupRemove &&
            CheckEmailMailGroupRemove &&
            CheckDomainDisjoin &&
            CheckSharedDriveRemove &&
            CheckWiFiDisable &&
            CheckPrinterRemove &&
            CheckScanFolderRemove &&
            CheckVPNDisable;

        /// <summary>
        /// Runs the CanStartItSetup operation.
        /// </summary>
        public bool CanStartItSetup() => Status == RequestStatus.PendingITApproval;

        /// <summary>
        /// Runs the CanRegenerateToken operation.
        /// </summary>
        public bool CanRegenerateToken() => Status == RequestStatus.PendingEmployeeSign;

        /// <summary>
        /// Runs the SignByHr operation.
        /// </summary>
        public void SignByHr(string signatureBase64, string signerName, string signatureHmac, DateTime signedAtUtc)
        {
            HRSignature = signatureBase64;
            HRSignerName = signerName;
            HRSignedAt = signedAtUtc;
            HRSignatureHmac = signatureHmac;
            RefreshStatusFromApprovals(signedAtUtc);
            UpdatedAt = signedAtUtc;
        }

        /// <summary>
        /// Runs the SignByManager operation.
        /// </summary>
        public void SignByManager(string signatureBase64, string signerName, string signatureHmac, DateTime signedAtUtc)
        {
            HODSignature = signatureBase64;
            HODSignerName = signerName;
            HODSignedAt = signedAtUtc;
            HODSignatureHmac = signatureHmac;
            RefreshStatusFromApprovals(signedAtUtc);
            UpdatedAt = signedAtUtc;
        }

        /// <summary>
        /// Runs the SignByIt operation.
        /// </summary>
        public void SignByIt(string signatureBase64, string signerName, string signatureHmac, DateTime signedAtUtc)
        {
            ITAdminSignature = signatureBase64;
            ITAdminSignerName = signerName;
            ITAdminSignedAt = signedAtUtc;
            ITAdminSignatureHmac = signatureHmac;
            RefreshStatusFromApprovals(signedAtUtc);
            UpdatedAt = signedAtUtc;
        }

        /// <summary>
        /// Applies changes for the ApplyStep2Checklist operation.
        /// </summary>
        public void ApplyStep2Checklist(OffboardingRequest source, DateTime updatedAtUtc)
        {
            ClearedLaptop = source.ClearedLaptop;
            ClearedMobile = source.ClearedMobile;
            ClearedKeys = source.ClearedKeys;
            ClearedOtherAssets = source.ClearedOtherAssets;
            ClearanceRemarks = source.ClearanceRemarks;
            AccountDisabledAD = source.AccountDisabledAD;
            EmailForwarded = source.EmailForwarded;
            ForwardToEmail = source.ForwardToEmail;
            M365LicenseRevoked = source.M365LicenseRevoked;
            SoftwareLicensesRevoked = source.SoftwareLicensesRevoked;
            CheckPhysicalClean = source.CheckPhysicalClean;
            CheckO365Deactivate = source.CheckO365Deactivate;
            CheckWindowsDisable = source.CheckWindowsDisable;
            CheckEmailSignOut = source.CheckEmailSignOut;
            CheckSpecificSoftwareDeactivate = source.CheckSpecificSoftwareDeactivate;
            CheckGeneralSoftwareDeactivate = source.CheckGeneralSoftwareDeactivate;
            CheckAbcGroupRemove = source.CheckAbcGroupRemove;
            CheckEmailMailGroupRemove = source.CheckEmailMailGroupRemove;
            CheckDomainDisjoin = source.CheckDomainDisjoin;
            CheckSharedDriveRemove = source.CheckSharedDriveRemove;
            CheckWiFiDisable = source.CheckWiFiDisable;
            CheckPrinterRemove = source.CheckPrinterRemove;
            CheckScanFolderRemove = source.CheckScanFolderRemove;
            CheckVPNDisable = source.CheckVPNDisable;
            ChecklistRemarks = source.ChecklistRemarks;
            LaptopReturned = source.LaptopReturned;
            AccessoriesReturned = source.AccessoriesReturned;
            IsHardwareDamaged = source.IsHardwareDamaged;
            HardwareNotes = source.HardwareNotes;
            MissingItemFines = source.MissingItemFines;
            FinesRemarks = source.FinesRemarks;
            DataBackupDone = source.DataBackupDone;
            EmailBackupDone = source.EmailBackupDone;
            EmailForwardingDone = source.EmailForwardingDone;
            LicenseRemovalDone = source.LicenseRemovalDone;
            FollowUpRemarks = source.FollowUpRemarks;
            UpdatedAt = updatedAtUtc;
        }

        /// <summary>
        /// Runs the CanBeDeletedBy operation.
        /// </summary>
        public bool CanBeDeletedBy(bool isHr)
        {
            var isSignedByHrOnly = !string.IsNullOrEmpty(HRSignature)
                && string.IsNullOrEmpty(HODSignature)
                && string.IsNullOrEmpty(ITAdminSignature);

            return Status == RequestStatus.Draft
                || Status == RequestStatus.Rejected
                || (isHr && isSignedByHrOnly);
        }

        /// <summary>
        /// Runs the CanBeRejected operation.
        /// </summary>
        public bool CanBeRejected() =>
            Status != RequestStatus.Completed &&
            Status != RequestStatus.Approved &&
            Status != RequestStatus.PendingEmployeeSign;

        /// <summary>
        /// Runs the Reject operation.
        /// </summary>
        public void Reject(string reason, string? rejectedByUserId, DateTime updatedAtUtc)
        {
            Status = RequestStatus.Rejected;
            RejectionReason = reason;
            RejectedByUserId = rejectedByUserId;
            UpdatedAt = updatedAtUtc;
        }

        /// <summary>
        /// Runs the RegenerateAccessToken operation.
        /// </summary>
        public void RegenerateAccessToken(DateTime updatedAtUtc)
        {
            AccessToken = Guid.NewGuid();
            UpdatedAt = updatedAtUtc;
        }

        /// <summary>
        /// Runs the RefreshStatusFromApprovals operation.
        /// </summary>
        private void RefreshStatusFromApprovals(DateTime signedAtUtc)
        {
            var hasHR = !string.IsNullOrEmpty(HRSignature);
            var hasHOD = !string.IsNullOrEmpty(HODSignature);
            var hasIT = !string.IsNullOrEmpty(ITAdminSignature);

            if (hasHR && hasHOD && hasIT)
            {
                Status = RequestStatus.PendingEmployeeSign;
            }
            else if (hasHR && hasHOD)
            {
                Status = RequestStatus.PendingITApproval;
            }
            else if (hasHR)
            {
                Status = string.IsNullOrEmpty(ApprovedByUserId)
                    ? RequestStatus.PendingApproval
                    : RequestStatus.PendingHODApproval;
            }
        }
    }
}


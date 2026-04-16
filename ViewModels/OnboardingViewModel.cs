using IT_Form.Models;
using IT_Form.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace IT_Form.ViewModels
{
    public class OnboardingViewModel
    {
        public int Id { get; set; }
        public int CurrentStep { get; set; } = 1;

        // --- Step 1: Employee Details ---
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Employee ID is required.")]
        [StringLength(20)]
        [Display(Name = "Employee ID")]
        public string EmployeeID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Designation is required.")]
        [StringLength(100)]
        [Display(Name = "Designation / Job Title")]
        public string Designation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        [StringLength(50)]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company is required.")]
        [StringLength(50)]
        [Display(Name = "Company")]
        public string Branch { get; set; } = string.Empty;

        [Required(ErrorMessage = "Desk number is required.")]
        [StringLength(50)]
        [Display(Name = "Desk Number")]
        public string DeskNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Joining date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime? JoinDate { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(30)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Head of Department is required.")]
        [StringLength(150)]
        [Display(Name = "Head of Department (HOD)")]
        public string HODEmail { get; set; } = string.Empty;

        // --- Step 3: Software & Email (Legacy Compatibility) ---
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
        public string? OtherSoftwareDetails { get; set; }

        public bool PrecastGroup { get; set; }
        // OtherGroup and OtherGroupDetails are already present

        public bool MailboxAccount { get; set; }
        public bool MailboxLogistic { get; set; }
        public bool MailboxInfo { get; set; }
        public bool MailboxAdmin { get; set; }
        public bool MailboxIntern { get; set; }
        public bool MailboxIT { get; set; }
        public bool MailboxSupervisor { get; set; }
        public bool MailboxStore { get; set; }

        // --- PDPA Consent ---
        public bool IsPdpaConsented { get; set; }

        // --- Step 2: IT Assets ---
        public List<AssetItemViewModel> IssuedAssets { get; set; } = new();
        public List<OnboardingAssetCategoryOptionViewModel> AssetCategoryOptions { get; set; } = new();

        // --- Step 3: Software & Email ---
        public List<int> SelectedStandardSoftwareIds { get; set; } = new();

        public List<int> SelectedSoftwareIds { get; set; } = new(); // Restored for Software License version selection

        // Email Groups
        public List<int> SelectedEmailGroupIds { get; set; } = new();
        public bool OtherGroup { get; set; }
        public string? OtherGroupDetails { get; set; }

        // Shared Mailboxes
        public List<int> SelectedSharedMailboxIds { get; set; } = new();
        public bool HasOtherSharedMailbox { get; set; }
        public string? OtherSharedMailboxDetails { get; set; }

        // --- Multi-step Remarks ---
        public string? AssetRemarks { get; set; }
        public string? SoftwareRemarks { get; set; }
        public string? FolderRemarks { get; set; }

        // --- Additional "Other" Specifications ---
        public string? OtherBranchDetails { get; set; }
        public string? OtherDepartmentDetails { get; set; }
        public bool HasOtherFolder { get; set; }
        public string? OtherFolderDetails { get; set; }

        // --- Step 4: Server Folder Permissions ---
        public List<FolderPermissionViewModel> FolderPermissions { get; set; } = new();

        // --- Step 5: Signatures ---
        public string? RequesterSignature { get; set; }
        public string? ITAdminSignature { get; set; }

        // --- IT Checklist (Step 5) ---
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
        public string? ChecklistRemarks { get; set; }

        /// <summary>
        /// Initialize default folder permissions list
        /// </summary>
        public void InitializeDefaultFolderPermissions(List<string>? dbFolderNames = null)
        {
            var defaultFolders = dbFolderNames ?? new List<string>();

            FolderPermissions = defaultFolders.Select(f => new FolderPermissionViewModel
            {
                FolderName = f,
                CanRead = false,
                CanModify = false
            }).ToList();
        }
    }

    public class AssetItemViewModel
    {
        public AssetType AssetType { get; set; }
        public string? OtherAssetType { get; set; }
        public int? ITAssetId { get; set; }
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }
        public string? AssetTag { get; set; }
        public int? Qty { get; set; }
        public string? BrandModel { get; set; }
        public bool IsChecked { get; set; }
    }

    public sealed class OnboardingAssetCategoryOptionViewModel
    {
        public AssetType AssetType { get; set; }
        public string? OtherAssetType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class FolderPermissionViewModel
    {
        public string FolderName { get; set; } = string.Empty;
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
    }
}

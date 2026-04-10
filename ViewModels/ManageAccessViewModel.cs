namespace IT_Form.ViewModels
{
    public sealed class ManageAccessViewModel
    {
        public int OnboardingId { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string? EmployeeID { get; set; }
        public string? Designation { get; set; }
        public string? CurrentDepartment { get; set; }
        public string? CurrentBranch { get; set; }
        public DateTime? JoinedAt { get; set; }

        public List<int> SelectedEmailGroupIds { get; set; } = new();
        public string? OtherGroupDetails { get; set; }

        public List<int> SelectedSharedMailboxIds { get; set; } = new();
        public string? OtherSharedMailboxDetails { get; set; }

        public List<FolderPermissionViewModel> FolderPermissions { get; set; } = new();
        public string? OtherFolderDetails { get; set; }

        public IReadOnlyList<ManageAccessEmailGroupOptionViewModel> AvailableEmailGroups { get; set; } = [];
        public IReadOnlyList<ManageAccessSharedMailboxOptionViewModel> AvailableSharedMailboxes { get; set; } = [];
    }

    public sealed class ManageAccessEmailGroupOptionViewModel
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupEmail { get; set; } = string.Empty;
    }

    public sealed class ManageAccessSharedMailboxOptionViewModel
    {
        public int Id { get; set; }
        public string MailboxName { get; set; } = string.Empty;
        public string MailboxAddress { get; set; } = string.Empty;
    }
}

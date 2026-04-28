// Project comment: ServerFolderAccessReportViewModel.cs is part of the IT Form application source code.

using System;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the ServerFolderAccessReport screen or form.
    /// </summary>
    public class ServerFolderAccessReportViewModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty; // "Onboarding"
        public int RequestId { get; set; }
        public string AccessLevel { get; set; } = string.Empty; // "Read" or "Read & Modify"
        public DateTime? DateGranted { get; set; }
    }
}

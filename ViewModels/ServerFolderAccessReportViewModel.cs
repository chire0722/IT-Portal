using System;

namespace IT_Form.ViewModels
{
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

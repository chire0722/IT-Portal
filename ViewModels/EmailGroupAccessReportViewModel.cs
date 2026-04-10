using System;

namespace IT_Form.ViewModels
{
    public class EmailGroupAccessReportViewModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string GroupEmail { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty; // "Onboarding"
        public int RequestId { get; set; }
        public DateTime? DateGranted { get; set; }
    }
}

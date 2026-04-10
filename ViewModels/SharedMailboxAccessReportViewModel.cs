using System;

namespace IT_Form.ViewModels
{
    public class SharedMailboxAccessReportViewModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string MailboxName { get; set; } = string.Empty;
        public string MailboxAddress { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty; // "Onboarding"
        public int RequestId { get; set; }
        public DateTime? DateGranted { get; set; }
    }
}

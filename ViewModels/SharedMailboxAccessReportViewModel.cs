// Project comment: SharedMailboxAccessReportViewModel.cs is part of the IT Form application source code.

using System;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the SharedMailboxAccessReport screen or form.
    /// </summary>
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

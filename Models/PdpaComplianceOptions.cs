// Project comment: PdpaComplianceOptions.cs is part of the IT Form application source code.

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the PdpaComplianceOptions class used by the IT Form application.
    /// </summary>
    public sealed class PdpaComplianceOptions
    {
        public string DataControllerName { get; set; } = "YTC / PYT Group";
        public string PrivacyContactEmail { get; set; } = "itdept@ytcprecast.com";
        public string BreachResponseEmail { get; set; } = "itdept@ytcprecast.com";
        public int RecordRetentionDays { get; set; } = 2555;
        public int SecurityTokenRetentionDays { get; set; } = 30;
        public int AuditLogRetentionDays { get; set; } = 2555;
    }
}

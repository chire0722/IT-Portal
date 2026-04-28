// Project comment: AuditLogClassifier.cs is part of the IT Form application source code.

using IT_Form.Models;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the AuditLogClassifier class used by the IT Form application.
    /// </summary>
    public static class AuditLogClassifier
    {
        /// <summary>
        /// Gets the data required by the GetSourceType operation.
        /// </summary>
        public static string GetSourceType(AuditLog log)
        {
            if (log.EntityName == "Command")
            {
                return "Command Pipeline";
            }

            if (IsSecurityEvent(log))
            {
                return "Security Event";
            }

            if (string.Equals(log.UserId, "System", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(log.PerformedBy, "System", StringComparison.OrdinalIgnoreCase))
            {
                return "System Event";
            }

            return "Entity Change";
        }

        /// <summary>
        /// Gets the data required by the GetStageLabel operation.
        /// </summary>
        public static string GetStageLabel(AuditLog log)
        {
            if (log.EntityName == "Command")
            {
                return "Business Command";
            }

            if (IsSecurityEvent(log))
            {
                return "Authentication";
            }

            return log.Action switch
            {
                "Created" => "Persistence",
                "Updated" => "Persistence",
                "Deleted" => "Persistence",
                "Rejected" => "Workflow",
                _ => "Application Event"
            };
        }

        /// <summary>
        /// Runs the IsSecurityEvent operation.
        /// </summary>
        private static bool IsSecurityEvent(AuditLog log)
        {
            if (log.EntityName == "User Account" || log.EntityName == nameof(ApplicationUser))
            {
                return true;
            }

            return log.Action.Contains("Login", StringComparison.OrdinalIgnoreCase)
                || log.Action.Contains("Logout", StringComparison.OrdinalIgnoreCase)
                || log.Action.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || log.Action.Contains("Otp", StringComparison.OrdinalIgnoreCase);
        }
    }
}

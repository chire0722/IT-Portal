// Project comment: SmtpSettings.cs is part of the IT Form application source code.

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SmtpSettings class used by the IT Form application.
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }
}


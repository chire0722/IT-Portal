// Project comment: WelcomeEmailMessage.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications
{
    /// <summary>
    /// Defines the WelcomeEmailMessage record used by the IT Form application.
    /// </summary>
    public sealed record WelcomeEmailMessage(string Email, string EmployeeName) : IEmailBackgroundMessage;
}

namespace IT_Form.Infrastructure.Notifications
{
    public sealed record WelcomeEmailMessage(string Email, string EmployeeName) : IEmailBackgroundMessage;
}

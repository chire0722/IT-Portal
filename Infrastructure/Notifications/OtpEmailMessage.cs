namespace IT_Form.Infrastructure.Notifications;

public sealed record OtpEmailMessage(
    string Email,
    string Subject,
    string HtmlBody) : IEmailBackgroundMessage;

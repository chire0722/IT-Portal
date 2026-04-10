namespace IT_Form.Infrastructure.Notifications;

public sealed record RenderedEmailMessage(
    string Email,
    string Subject,
    string HtmlBody,
    string Cc = "",
    string? SignatureBase64 = null,
    bool BypassActiveCheck = false) : IEmailBackgroundMessage;

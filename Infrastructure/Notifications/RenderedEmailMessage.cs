// Project comment: RenderedEmailMessage.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications;

/// <summary>
/// Defines the RenderedEmailMessage record used by the IT Form application.
/// </summary>
public sealed record RenderedEmailMessage(
    string Email,
    string Subject,
    string HtmlBody,
    string Cc = "",
    string? SignatureBase64 = null,
    bool BypassActiveCheck = false) : IEmailBackgroundMessage;

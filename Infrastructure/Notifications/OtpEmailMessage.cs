// Project comment: OtpEmailMessage.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications;

/// <summary>
/// Defines the OtpEmailMessage record used by the IT Form application.
/// </summary>
public sealed record OtpEmailMessage(
    string Email,
    string Subject,
    string HtmlBody) : IEmailBackgroundMessage;

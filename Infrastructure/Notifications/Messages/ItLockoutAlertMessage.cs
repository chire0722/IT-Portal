// Project comment: ItLockoutAlertMessage.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications;

/// <summary>
/// Defines the ItLockoutAlertMessage record used by the IT Form application.
/// </summary>
public sealed record ItLockoutAlertMessage(
    string Email,
    string UserId,
    DateTime Timestamp) : IEmailBackgroundMessage;

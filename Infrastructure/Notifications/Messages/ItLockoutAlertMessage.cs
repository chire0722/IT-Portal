namespace IT_Form.Infrastructure.Notifications;

public sealed record ItLockoutAlertMessage(
    string Email,
    string UserId,
    DateTime Timestamp) : IEmailBackgroundMessage;

// Project comment: IEmailBackgroundMessage.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications;

/// <summary>
/// Defines the contract implemented by IEmailBackgroundMessage services.
/// </summary>
public interface IEmailBackgroundMessage
{
    string Email { get; }
}

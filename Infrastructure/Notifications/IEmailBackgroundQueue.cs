// Project comment: IEmailBackgroundQueue.cs is part of the IT Form application source code.

namespace IT_Form.Infrastructure.Notifications
{
    /// <summary>
    /// Defines the contract implemented by IEmailBackgroundQueue services.
    /// </summary>
    public interface IEmailBackgroundQueue
    {
        ValueTask QueueEmailAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken = default);
    }
}

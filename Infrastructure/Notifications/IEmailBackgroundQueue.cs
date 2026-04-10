namespace IT_Form.Infrastructure.Notifications
{
    public interface IEmailBackgroundQueue
    {
        ValueTask QueueEmailAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken = default);
    }
}

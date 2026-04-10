using IT_Form.Data;

namespace IT_Form.Infrastructure.Notifications
{
    public sealed class EmailBackgroundQueue : IEmailBackgroundQueue
    {
        private readonly ApplicationDbContext _context;

        public EmailBackgroundQueue(ApplicationDbContext context)
        {
            _context = context;
        }

        public ValueTask QueueEmailAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            _context.EmailOutboxMessages.Add(EmailOutboxMessageSerializer.Serialize(message));
            return ValueTask.CompletedTask;
        }
    }
}

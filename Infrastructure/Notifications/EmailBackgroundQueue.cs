// Project comment: EmailBackgroundQueue.cs is part of the IT Form application source code.

using IT_Form.Data;

namespace IT_Form.Infrastructure.Notifications
{
    /// <summary>
    /// Defines the EmailBackgroundQueue class used by the IT Form application.
    /// </summary>
    public sealed class EmailBackgroundQueue : IEmailBackgroundQueue
    {
        private readonly ApplicationDbContext _context;

        public EmailBackgroundQueue(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Runs the QueueEmailAsync operation.
        /// </summary>
        public ValueTask QueueEmailAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            _context.EmailOutboxMessages.Add(EmailOutboxMessageSerializer.Serialize(message));
            return ValueTask.CompletedTask;
        }
    }
}

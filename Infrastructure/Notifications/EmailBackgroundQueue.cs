// Project comment: EmailBackgroundQueue.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Services;

namespace IT_Form.Infrastructure.Notifications
{
    /// <summary>
    /// Defines the EmailBackgroundQueue class used by the IT Form application.
    /// </summary>
    public sealed class EmailBackgroundQueue : IEmailBackgroundQueue
    {
        private readonly ApplicationDbContext _context;
        private readonly ILicenseEncryptionService _encryptionService;

        public EmailBackgroundQueue(
            ApplicationDbContext context,
            ILicenseEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Runs the QueueEmailAsync operation.
        /// </summary>
        public ValueTask QueueEmailAsync(IEmailBackgroundMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            _context.EmailOutboxMessages.Add(EmailOutboxMessageSerializer.Serialize(message, _encryptionService));
            return ValueTask.CompletedTask;
        }
    }
}


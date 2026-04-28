// Project comment: UpdateSoftwareKeyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Services;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    /// <summary>
    /// Handles the UpdateSoftwareKeyCommand command workflow.
    /// </summary>
    public sealed class UpdateSoftwareKeyCommandHandler : IRequestHandler<UpdateSoftwareKeyCommand, UpdateSoftwareKeyResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILicenseEncryptionService _encryptionService;

        public UpdateSoftwareKeyCommandHandler(
            ApplicationDbContext context,
            ILicenseEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<UpdateSoftwareKeyResult> Handle(UpdateSoftwareKeyCommand request, CancellationToken cancellationToken)
        {
            var key = await _context.SoftwareKeys.FindAsync(new object[] { request.Id }, cancellationToken);
            if (key == null)
            {
                return new UpdateSoftwareKeyResult(UpdateSoftwareKeyStatus.NotFound, "Key not found.");
            }

            key.KeyValue = _encryptionService.EncryptKey(request.KeyValue.Trim());
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.KeyValue.Trim()));
            key.KeyValueHash = Convert.ToBase64String(bytes);

            await _context.SaveChangesAsync(cancellationToken);
            return new UpdateSoftwareKeyResult(UpdateSoftwareKeyStatus.Updated, "Key updated.");
        }
    }
}

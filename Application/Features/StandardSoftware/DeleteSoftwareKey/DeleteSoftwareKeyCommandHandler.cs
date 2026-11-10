// Project comment: DeleteSoftwareKeyCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    /// <summary>
    /// Handles the DeleteSoftwareKeyCommand command workflow.
    /// </summary>
    public sealed class DeleteSoftwareKeyCommandHandler : IRequestHandler<DeleteSoftwareKeyCommand, DeleteSoftwareKeyResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly IT_Form.Application.Common.Abstractions.ICurrentUserAccessor _currentUserAccessor;

        public DeleteSoftwareKeyCommandHandler(
            ApplicationDbContext context,
            IT_Form.Application.Common.Abstractions.ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<DeleteSoftwareKeyResult> Handle(DeleteSoftwareKeyCommand request, CancellationToken cancellationToken)
        {
            var key = await _context.SoftwareKeys
                .Include(item => item.SoftwareLicense)
                .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
            if (key == null)
            {
                return new DeleteSoftwareKeyResult(DeleteSoftwareKeyStatus.NotFound, "Key not found.");
            }

            if (!await StandardSoftwareLicenseScopeSupport.CanAccessAsync(
                    _context, _currentUserAccessor.User, key.SoftwareLicenseId, cancellationToken))
            {
                return new DeleteSoftwareKeyResult(DeleteSoftwareKeyStatus.NotFound, "Key not found.");
            }

            if (key.IsAssigned)
            {
                return new DeleteSoftwareKeyResult(DeleteSoftwareKeyStatus.AssignedBlocked, "Cannot delete an assigned key.");
            }

            if (key.SoftwareLicense != null && key.SoftwareLicense.IsSubscription && !key.SoftwareLicense.IsFree)
            {
                key.SoftwareLicense.ContractLimit = Math.Max(key.SoftwareLicense.ContractLimit - 1, 0);
                key.SoftwareLicense.TotalLicenses = key.SoftwareLicense.ContractLimit;
            }

            _context.SoftwareKeys.Remove(key);
            await _context.SaveChangesAsync(cancellationToken);
            return new DeleteSoftwareKeyResult(DeleteSoftwareKeyStatus.Deleted, "License key deleted.");
        }
    }
}

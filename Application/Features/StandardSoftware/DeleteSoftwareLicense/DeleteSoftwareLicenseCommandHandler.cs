// Project comment: DeleteSoftwareLicenseCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    /// <summary>
    /// Handles the DeleteSoftwareLicenseCommand command workflow.
    /// </summary>
    public sealed class DeleteSoftwareLicenseCommandHandler : IRequestHandler<DeleteSoftwareLicenseCommand, DeleteSoftwareLicenseResult>
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteSoftwareLicenseCommandHandler(ApplicationDbContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Executes the request and returns the workflow result.
        /// </summary>
        public async Task<DeleteSoftwareLicenseResult> Handle(DeleteSoftwareLicenseCommand request, CancellationToken cancellationToken)
        {
            var license = await _context.SoftwareLicenses
                .Include(l => l.Keys)
                .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

            if (license == null)
            {
                return new DeleteSoftwareLicenseResult(DeleteSoftwareLicenseStatus.NotFound, "Version not found.");
            }

            if (!await StandardSoftwareLicenseScopeSupport.CanAccessAsync(
                    _context, _currentUserAccessor.User, license.Id, cancellationToken))
            {
                return new DeleteSoftwareLicenseResult(DeleteSoftwareLicenseStatus.NotFound, "Version not found.");
            }

            if (license.Keys != null && license.Keys.Any(k => k.IsAssigned))
            {
                return new DeleteSoftwareLicenseResult(DeleteSoftwareLicenseStatus.HasAssignedKeys, "Cannot delete version. It has currently assigned seats/keys.");
            }

            license.IsDeleted = true;
            license.DeletedAt = DateTime.UtcNow;
            if (license.Keys != null)
            {
                foreach (var key in license.Keys)
                {
                    key.IsDeleted = true;
                    key.DeletedAt = DateTime.UtcNow;
                }
            }

            _context.Update(license);
            await _context.SaveChangesAsync(cancellationToken);
            return new DeleteSoftwareLicenseResult(DeleteSoftwareLicenseStatus.Deleted, "License edition removed.");
        }
    }
}

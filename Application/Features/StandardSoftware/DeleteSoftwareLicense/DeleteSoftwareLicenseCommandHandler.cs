using IT_Form.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    public sealed class DeleteSoftwareLicenseCommandHandler : IRequestHandler<DeleteSoftwareLicenseCommand, DeleteSoftwareLicenseResult>
    {
        private readonly ApplicationDbContext _context;

        public DeleteSoftwareLicenseCommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DeleteSoftwareLicenseResult> Handle(DeleteSoftwareLicenseCommand request, CancellationToken cancellationToken)
        {
            var license = await _context.SoftwareLicenses
                .Include(l => l.Keys)
                .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

            if (license == null)
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

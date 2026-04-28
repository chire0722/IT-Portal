// Project comment: DeleteStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;

/// <summary>
/// Handles the DeleteStandardSoftwareCommand command workflow.
/// </summary>
public sealed class DeleteStandardSoftwareCommandHandler : IRequestHandler<DeleteStandardSoftwareCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public DeleteStandardSoftwareCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(DeleteStandardSoftwareCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StandardSoftware
            .Include(software => software.Licenses)
                .ThenInclude(license => license.Keys)
            .FirstOrDefaultAsync(software => software.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StandardSoftware), request.Id);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId ?? "Unknown";

        foreach (var license in entity.Licenses)
        {
            license.IsDeleted = true;
            license.DeletedAt = DateTime.UtcNow;

            foreach (var key in license.Keys)
            {
                key.IsDeleted = true;
                key.DeletedAt = DateTime.UtcNow;
            }
        }

        _context.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

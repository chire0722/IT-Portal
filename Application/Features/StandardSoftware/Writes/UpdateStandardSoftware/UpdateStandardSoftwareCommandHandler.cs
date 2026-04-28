// Project comment: UpdateStandardSoftwareCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

/// <summary>
/// Handles the UpdateStandardSoftwareCommand command workflow.
/// </summary>
public sealed class UpdateStandardSoftwareCommandHandler : IRequestHandler<UpdateStandardSoftwareCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public UpdateStandardSoftwareCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(UpdateStandardSoftwareCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StandardSoftware
            .FirstOrDefaultAsync(software => software.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StandardSoftware), request.Id);

        request.Adapt(entity);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

public sealed class UpdateStandardSoftwareCommandHandler : IRequestHandler<UpdateStandardSoftwareCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public UpdateStandardSoftwareCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

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

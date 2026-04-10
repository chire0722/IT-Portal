using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;

public sealed class UpdateEmailContactCommandHandler : IRequestHandler<UpdateEmailContactCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public UpdateEmailContactCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateEmailContactCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EmailContacts
            .FirstOrDefaultAsync(contact => contact.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailContact), request.Id);

        request.Adapt(entity);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

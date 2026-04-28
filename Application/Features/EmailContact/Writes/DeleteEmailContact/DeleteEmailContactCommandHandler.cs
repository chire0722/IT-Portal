// Project comment: DeleteEmailContactCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact.Writes.DeleteEmailContact;

/// <summary>
/// Handles the DeleteEmailContactCommand command workflow.
/// </summary>
public sealed class DeleteEmailContactCommandHandler : IRequestHandler<DeleteEmailContactCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public DeleteEmailContactCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<Unit> Handle(DeleteEmailContactCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.EmailContacts
            .FirstOrDefaultAsync(contact => contact.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(EmailContact), request.Id);

        _context.EmailContacts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

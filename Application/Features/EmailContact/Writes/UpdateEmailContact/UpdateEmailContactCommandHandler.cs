// Project comment: UpdateEmailContactCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Exceptions;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;

/// <summary>
/// Handles the UpdateEmailContactCommand command workflow.
/// </summary>
public sealed class UpdateEmailContactCommandHandler : IRequestHandler<UpdateEmailContactCommand, Unit>
{
    private readonly ApplicationDbContext _context;

    public UpdateEmailContactCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
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

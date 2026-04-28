// Project comment: CreateEmailContactCommandHandler.cs is part of the IT Form application source code.

using IT_Form.Data;
using Mapster;
using MediatR;
using EmailContactEntity = IT_Form.Models.EmailContact;

namespace IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;

/// <summary>
/// Handles the CreateEmailContactCommand command workflow.
/// </summary>
public sealed class CreateEmailContactCommandHandler : IRequestHandler<CreateEmailContactCommand, int>
{
    private readonly ApplicationDbContext _context;

    public CreateEmailContactCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<int> Handle(CreateEmailContactCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Adapt<EmailContactEntity>();
        entity.CreatedAt = DateTime.UtcNow;

        _context.EmailContacts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

using IT_Form.Data;
using Mapster;
using MediatR;
using EmailContactEntity = IT_Form.Models.EmailContact;

namespace IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;

public sealed class CreateEmailContactCommandHandler : IRequestHandler<CreateEmailContactCommand, int>
{
    private readonly ApplicationDbContext _context;

    public CreateEmailContactCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateEmailContactCommand request, CancellationToken cancellationToken)
    {
        var entity = request.Adapt<EmailContactEntity>();
        entity.CreatedAt = DateTime.UtcNow;

        _context.EmailContacts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}

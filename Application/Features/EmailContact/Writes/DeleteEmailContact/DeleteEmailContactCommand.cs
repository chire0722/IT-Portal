using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.DeleteEmailContact;

public sealed record DeleteEmailContactCommand(int Id) : IRequest<Unit>;

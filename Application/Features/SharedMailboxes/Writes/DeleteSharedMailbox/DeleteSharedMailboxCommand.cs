using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.DeleteSharedMailbox;

public sealed record DeleteSharedMailboxCommand(int Id) : IRequest<Unit>;

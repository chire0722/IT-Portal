using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.DeleteEmailGroup;

public sealed record DeleteEmailGroupCommand(int Id) : IRequest<Unit>;

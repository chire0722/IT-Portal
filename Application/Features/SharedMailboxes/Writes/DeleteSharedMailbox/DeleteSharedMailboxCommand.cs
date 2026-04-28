// Project comment: DeleteSharedMailboxCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.DeleteSharedMailbox;

/// <summary>
/// Carries data needed to execute the DeleteSharedMailbox command.
/// </summary>
public sealed record DeleteSharedMailboxCommand(int Id) : IRequest<Unit>;

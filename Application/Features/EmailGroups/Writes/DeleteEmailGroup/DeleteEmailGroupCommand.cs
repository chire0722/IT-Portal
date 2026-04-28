// Project comment: DeleteEmailGroupCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.DeleteEmailGroup;

/// <summary>
/// Carries data needed to execute the DeleteEmailGroup command.
/// </summary>
public sealed record DeleteEmailGroupCommand(int Id) : IRequest<Unit>;

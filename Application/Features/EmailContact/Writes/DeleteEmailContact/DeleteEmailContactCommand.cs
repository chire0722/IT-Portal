// Project comment: DeleteEmailContactCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.DeleteEmailContact;

/// <summary>
/// Carries data needed to execute the DeleteEmailContact command.
/// </summary>
public sealed record DeleteEmailContactCommand(int Id) : IRequest<Unit>;

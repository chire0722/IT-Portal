// Project comment: LogoutCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.Writes.Logout;

/// <summary>
/// Carries data needed to execute the Logout command.
/// </summary>
public sealed record LogoutCommand() : IRequest<Unit>;

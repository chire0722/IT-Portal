// Project comment: UpdateEmailContactCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;

/// <summary>
/// Carries data needed to execute the UpdateEmailContact command.
/// </summary>
public sealed record UpdateEmailContactCommand(
    int Id,
    string Name,
    string EmailAddress,
    string? Department) : IRequest<Unit>;

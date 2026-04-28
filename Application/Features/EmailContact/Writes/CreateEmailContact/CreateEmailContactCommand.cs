// Project comment: CreateEmailContactCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;

/// <summary>
/// Carries data needed to execute the CreateEmailContact command.
/// </summary>
public sealed record CreateEmailContactCommand(
    string Name,
    string EmailAddress,
    string? Department) : IRequest<int>;

// Project comment: UpdateSignatureCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.Writes.UpdateSignature;

/// <summary>
/// Carries data needed to execute the UpdateSignature command.
/// </summary>
public sealed record UpdateSignatureCommand(
    string UserId,
    string? SignatureData) : IRequest<Unit>;

// Project comment: DeleteStandardSoftwareCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;

/// <summary>
/// Carries data needed to execute the DeleteStandardSoftware command.
/// </summary>
public sealed record DeleteStandardSoftwareCommand(
    int Id,
    string? DeletedByUserId) : IRequest<Unit>;

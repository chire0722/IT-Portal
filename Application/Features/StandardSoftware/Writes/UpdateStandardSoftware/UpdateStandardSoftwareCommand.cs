// Project comment: UpdateStandardSoftwareCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

/// <summary>
/// Carries data needed to execute the UpdateStandardSoftware command.
/// </summary>
public sealed record UpdateStandardSoftwareCommand(
    int Id,
    string SoftwareName,
    bool IsMandatory,
    bool IsFree,
    bool IsActive,
    bool HasMultipleVersions) : IRequest<Unit>;

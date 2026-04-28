// Project comment: CreateStandardSoftwareCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;

/// <summary>
/// Carries data needed to execute the CreateStandardSoftware command.
/// </summary>
public sealed record CreateStandardSoftwareCommand(
    string SoftwareName,
    bool IsMandatory,
    bool IsFree,
    bool IsActive,
    bool HasMultipleVersions) : IRequest<int>;

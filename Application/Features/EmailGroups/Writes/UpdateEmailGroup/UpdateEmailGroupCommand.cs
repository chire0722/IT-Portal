// Project comment: UpdateEmailGroupCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup;

/// <summary>
/// Carries data needed to execute the UpdateEmailGroup command.
/// </summary>
public sealed record UpdateEmailGroupCommand(
    int Id,
    string GroupName,
    string GroupEmail,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

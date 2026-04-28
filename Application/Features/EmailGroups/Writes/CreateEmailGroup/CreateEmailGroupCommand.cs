// Project comment: CreateEmailGroupCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup;

/// <summary>
/// Carries data needed to execute the CreateEmailGroup command.
/// </summary>
public sealed record CreateEmailGroupCommand(
    string GroupName,
    string GroupEmail,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

// Project comment: UpdateSharedMailboxCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox;

/// <summary>
/// Carries data needed to execute the UpdateSharedMailbox command.
/// </summary>
public sealed record UpdateSharedMailboxCommand(
    int Id,
    string MailboxName,
    string MailboxAddress,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

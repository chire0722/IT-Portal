// Project comment: CreateSharedMailboxCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox;

/// <summary>
/// Carries data needed to execute the CreateSharedMailbox command.
/// </summary>
public sealed record CreateSharedMailboxCommand(
    string MailboxName,
    string MailboxAddress,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

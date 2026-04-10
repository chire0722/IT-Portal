using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox;

public sealed record UpdateSharedMailboxCommand(
    int Id,
    string MailboxName,
    string MailboxAddress,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

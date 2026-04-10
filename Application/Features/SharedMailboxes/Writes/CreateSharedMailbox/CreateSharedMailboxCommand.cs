using MediatR;

namespace IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox;

public sealed record CreateSharedMailboxCommand(
    string MailboxName,
    string MailboxAddress,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

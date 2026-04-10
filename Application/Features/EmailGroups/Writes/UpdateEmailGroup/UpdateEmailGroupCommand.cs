using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup;

public sealed record UpdateEmailGroupCommand(
    int Id,
    string GroupName,
    string GroupEmail,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

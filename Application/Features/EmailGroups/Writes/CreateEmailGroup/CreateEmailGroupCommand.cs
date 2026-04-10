using MediatR;

namespace IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup;

public sealed record CreateEmailGroupCommand(
    string GroupName,
    string GroupEmail,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

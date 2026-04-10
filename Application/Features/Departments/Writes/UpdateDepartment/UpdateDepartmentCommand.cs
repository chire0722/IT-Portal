using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.UpdateDepartment;

public sealed record UpdateDepartmentCommand(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

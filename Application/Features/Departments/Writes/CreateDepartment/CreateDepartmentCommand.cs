using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.CreateDepartment;

public sealed record CreateDepartmentCommand(
    string Name,
    string? Description,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

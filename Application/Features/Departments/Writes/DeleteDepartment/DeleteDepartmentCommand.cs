using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.DeleteDepartment;

public sealed record DeleteDepartmentCommand(int Id) : IRequest<Unit>;

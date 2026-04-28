// Project comment: DeleteDepartmentCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.DeleteDepartment;

/// <summary>
/// Carries data needed to execute the DeleteDepartment command.
/// </summary>
public sealed record DeleteDepartmentCommand(int Id) : IRequest<Unit>;

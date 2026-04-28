// Project comment: UpdateDepartmentCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.UpdateDepartment;

/// <summary>
/// Carries data needed to execute the UpdateDepartment command.
/// </summary>
public sealed record UpdateDepartmentCommand(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<Unit>;

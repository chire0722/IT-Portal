// Project comment: CreateDepartmentCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Departments.Writes.CreateDepartment;

/// <summary>
/// Carries data needed to execute the CreateDepartment command.
/// </summary>
public sealed record CreateDepartmentCommand(
    string Name,
    string? Description,
    bool IsActive,
    int[] SelectedCompanyIds) : IRequest<int>;

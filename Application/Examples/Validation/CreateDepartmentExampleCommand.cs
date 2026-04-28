// Project comment: CreateDepartmentExampleCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Examples.Validation;

/// <summary>
/// Carries data needed to execute the CreateDepartmentExample command.
/// </summary>
public sealed record CreateDepartmentExampleCommand(
    string Name,
    string? Description) : IRequest<Unit>;

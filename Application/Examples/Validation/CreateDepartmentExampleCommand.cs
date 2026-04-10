using MediatR;

namespace IT_Form.Application.Examples.Validation;

public sealed record CreateDepartmentExampleCommand(
    string Name,
    string? Description) : IRequest<Unit>;

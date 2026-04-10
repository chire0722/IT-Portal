using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;

public sealed record ToggleUserManagementActiveCommand(string Id) : IRequest<ToggleUserManagementActiveResult>;

public sealed record ToggleUserManagementActiveResult(string FullName, bool IsActive);

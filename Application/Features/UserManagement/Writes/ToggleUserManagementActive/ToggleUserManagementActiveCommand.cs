// Project comment: ToggleUserManagementActiveCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.ToggleUserManagementActive;

/// <summary>
/// Carries data needed to execute the ToggleUserManagementActive command.
/// </summary>
public sealed record ToggleUserManagementActiveCommand(string Id) : IRequest<ToggleUserManagementActiveResult>;

/// <summary>
/// Represents the result returned by the ToggleUserManagementActive workflow.
/// </summary>
public sealed record ToggleUserManagementActiveResult(string FullName, bool IsActive);

// Project comment: DeleteUserManagementCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;

/// <summary>
/// Carries data needed to execute the DeleteUserManagement command.
/// </summary>
public sealed record DeleteUserManagementCommand(string Id) : IRequest<string>;

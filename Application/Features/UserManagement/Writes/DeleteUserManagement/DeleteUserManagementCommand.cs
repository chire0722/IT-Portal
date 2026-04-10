using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.DeleteUserManagement;

public sealed record DeleteUserManagementCommand(string Id) : IRequest<string>;

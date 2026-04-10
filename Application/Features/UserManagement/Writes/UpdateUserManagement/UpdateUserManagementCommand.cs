using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement;

public sealed record UpdateUserManagementCommand(
    string Id,
    string FullName,
    string Username,
    string? Email,
    string? Department,
    string? Password,
    string? ConfirmPassword,
    bool IsIT,
    bool IsHR,
    bool IsHOD,
    bool IsActive,
    List<string>? SelectedCompanies) : IRequest<Unit>;

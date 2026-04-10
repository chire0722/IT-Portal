using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

public sealed record CreateUserManagementCommand(
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
    List<string>? SelectedCompanies) : IRequest<string>;

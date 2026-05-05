// Project comment: UpdateUserManagementCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.UpdateUserManagement;

/// <summary>
/// Carries data needed to execute the UpdateUserManagement command.
/// </summary>
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
    List<string>? SelectedCompanies,
    bool IsSuperAdmin = false) : IRequest<Unit>;

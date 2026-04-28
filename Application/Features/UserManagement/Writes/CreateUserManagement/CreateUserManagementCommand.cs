// Project comment: CreateUserManagementCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.UserManagement.Writes.CreateUserManagement;

/// <summary>
/// Carries data needed to execute the CreateUserManagement command.
/// </summary>
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

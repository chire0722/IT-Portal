using IT_Form.ViewModels;

namespace IT_Form.Application.Features.UserManagement;

public sealed class UserManagementIndexListItem
{
    public string Id { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Department { get; init; }
    public bool IsActive { get; init; }
    public List<string> Titles { get; init; } = [];
    public List<string> Companies { get; init; } = [];
}

public sealed class UserManagementCompanyOption
{
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class UserManagementDepartmentOption
{
    public string Name { get; init; } = string.Empty;
}

public sealed class UserManagementEditorPageViewModel : UserManagementViewModel
{
    public List<UserManagementDepartmentOption> Departments { get; init; } = [];
    public List<UserManagementCompanyOption> Companies { get; init; } = [];
}

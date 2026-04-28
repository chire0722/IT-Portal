// Project comment: EmailGroupPageModels.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using IT_Form.Application.Common.Models;
using IT_Form.Helpers;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.EmailGroup;

/// <summary>
/// Defines the EmailGroupIndexListItem class used by the IT Form application.
/// </summary>
public sealed class EmailGroupIndexListItem
{
    public int Id { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string GroupEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Companies { get; init; } = [];
}

/// <summary>
/// Provides view data for the EmailGroupIndexPage screen or form.
/// </summary>
public sealed class EmailGroupIndexPageViewModel
{
    public int? SelectedCompanyId { get; init; }
    public List<PageCompanyOption> CompanyOptions { get; init; } = [];
    public List<EmailGroupIndexListItem> Items { get; init; } = [];
}

/// <summary>
/// Provides view data for the EmailGroupEditorPage screen or form.
/// </summary>
public sealed class EmailGroupEditorPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Group Name is required")]
    [StringLength(50)]
    [Display(Name = "Group Name")]
    public string GroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required")]
    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Group Email")]
    public string GroupEmail { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> SelectedCompanyIds { get; set; } = [];

    public List<PageCompanyOption> CompanyOptions { get; set; } = [];
}

/// <summary>
/// Provides view data for the EmailGroupMembersPage screen or form.
/// </summary>
public sealed class EmailGroupMembersPageViewModel
{
    public int GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string GroupEmail { get; init; } = string.Empty;
    public int? CurrentEmployeeId { get; init; }
    public string? CurrentEmployeeDisplayName { get; init; }
    public List<PageCurrentEmployeeOption> CurrentEmployees { get; init; } = [];
    public List<EmailGroupMemberListItem> Members { get; init; } = [];
}

/// <summary>
/// Defines the EmailGroupMemberListItem class used by the IT Form application.
/// </summary>
public sealed class EmailGroupMemberListItem
{
    public string EmployeeName { get; init; } = "Unknown";
    public string Department { get; init; } = "-";
    public string RequestType { get; init; } = string.Empty;
    public DateTime? RequestDate { get; init; }
    public RequestStatus? Status { get; init; }
    public int RequestId { get; init; }
    public string ControllerName { get; init; } = string.Empty;
}

/// <summary>
/// Provides view data for the EmailGroupReportPage screen or form.
/// </summary>
public sealed class EmailGroupReportPageViewModel
{
    public PaginatedList<IT_Form.ViewModels.EmailGroupAccessReportViewModel> Report { get; init; }
        = new([], 0, 1, 15);

    public string? CurrentFilter { get; init; }
    public int? GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;

    public string GroupNameDisplay => string.IsNullOrWhiteSpace(GroupName) ? "All Email Groups" : GroupName;
    public bool ShowGroupColumn => string.IsNullOrWhiteSpace(GroupName);
    public int EmptyStateColumnCount => ShowGroupColumn ? 7 : 6;
}

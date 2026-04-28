// Project comment: SharedMailboxPageModels.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using IT_Form.Application.Common.Models;
using IT_Form.Helpers;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.SharedMailbox;

/// <summary>
/// Defines the SharedMailboxIndexListItem class used by the IT Form application.
/// </summary>
public sealed class SharedMailboxIndexListItem
{
    public int Id { get; init; }
    public string MailboxName { get; init; } = string.Empty;
    public string MailboxAddress { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Companies { get; init; } = [];
}

/// <summary>
/// Provides view data for the SharedMailboxIndexPage screen or form.
/// </summary>
public sealed class SharedMailboxIndexPageViewModel
{
    public int? SelectedCompanyId { get; init; }
    public List<PageCompanyOption> CompanyOptions { get; init; } = [];
    public List<SharedMailboxIndexListItem> Items { get; init; } = [];
}

/// <summary>
/// Provides view data for the SharedMailboxEditorPage screen or form.
/// </summary>
public sealed class SharedMailboxEditorPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mailbox Name is required")]
    [StringLength(50)]
    [Display(Name = "Mailbox Name")]
    public string MailboxName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mailbox Address is required")]
    [StringLength(100)]
    [EmailAddress]
    [Display(Name = "Mailbox Address")]
    public string MailboxAddress { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> SelectedCompanyIds { get; set; } = [];

    public List<PageCompanyOption> CompanyOptions { get; set; } = [];
}

/// <summary>
/// Provides view data for the SharedMailboxMembersPage screen or form.
/// </summary>
public sealed class SharedMailboxMembersPageViewModel
{
    public int MailboxId { get; init; }
    public string MailboxName { get; init; } = string.Empty;
    public string MailboxAddress { get; init; } = string.Empty;
    public int? CurrentEmployeeId { get; init; }
    public string? CurrentEmployeeDisplayName { get; init; }
    public List<PageCurrentEmployeeOption> CurrentEmployees { get; init; } = [];
    public List<SharedMailboxMemberListItem> Members { get; init; } = [];
}

/// <summary>
/// Defines the SharedMailboxMemberListItem class used by the IT Form application.
/// </summary>
public sealed class SharedMailboxMemberListItem
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
/// Provides view data for the SharedMailboxReportPage screen or form.
/// </summary>
public sealed class SharedMailboxReportPageViewModel
{
    public PaginatedList<IT_Form.ViewModels.SharedMailboxAccessReportViewModel> Report { get; init; }
        = new([], 0, 1, 15);

    public string? CurrentFilter { get; init; }
    public int? MailboxId { get; init; }
    public string MailboxName { get; init; } = string.Empty;

    public string MailboxNameDisplay => string.IsNullOrWhiteSpace(MailboxName) ? "All Shared Mailboxes" : MailboxName;
    public bool ShowMailboxColumn => string.IsNullOrWhiteSpace(MailboxName);
    public int EmptyStateColumnCount => ShowMailboxColumn ? 7 : 6;
}

using System.ComponentModel.DataAnnotations;
using IT_Form.Application.Common.Models;
using IT_Form.Helpers;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.ServerFolder;

public sealed class ServerFolderIndexListItem
{
    public int Id { get; init; }
    public string FolderName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Companies { get; init; } = [];
}

public sealed class ServerFolderEditorPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Folder Name is required")]
    [StringLength(100)]
    [Display(Name = "Folder Name")]
    public string FolderName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<int> SelectedCompanyIds { get; set; } = [];

    public List<PageCompanyOption> CompanyOptions { get; set; } = [];
}

public sealed class ServerFolderMembersPageViewModel
{
    public int FolderId { get; init; }
    public string FolderName { get; init; } = string.Empty;
    public int? CurrentEmployeeId { get; init; }
    public string? CurrentEmployeeDisplayName { get; init; }
    public List<PageCurrentEmployeeOption> CurrentEmployees { get; init; } = [];
    public List<ServerFolderMemberListItem> Members { get; init; } = [];
}

public sealed class ServerFolderMemberListItem
{
    public string EmployeeName { get; init; } = "Unknown";
    public string Department { get; init; } = "-";
    public bool CanRead { get; init; }
    public bool CanModify { get; init; }
    public string RequestType { get; init; } = string.Empty;
    public DateTime? RequestDate { get; init; }
    public RequestStatus? Status { get; init; }
    public int RequestId { get; init; }
    public string ControllerName { get; init; } = string.Empty;
}

public sealed class ServerFolderReportPageViewModel
{
    public PaginatedList<IT_Form.ViewModels.ServerFolderAccessReportViewModel> Report { get; init; }
        = new([], 0, 1, 15);

    public string? CurrentFilter { get; init; }
    public int? FolderId { get; init; }
    public string FolderName { get; init; } = string.Empty;

    public string FolderNameDisplay => string.IsNullOrWhiteSpace(FolderName) ? "All Server Folders" : FolderName;
    public bool ShowFolderColumn => string.IsNullOrWhiteSpace(FolderName);
    public int EmptyStateColumnCount => ShowFolderColumn ? 8 : 7;
}

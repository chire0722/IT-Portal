// Project comment: DepartmentPageModels.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using IT_Form.Application.Common.Models;

namespace IT_Form.Application.Features.Department;

/// <summary>
/// Defines the DepartmentIndexListItem class used by the IT Form application.
/// </summary>
public sealed class DepartmentIndexListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Companies { get; init; } = [];
}

/// <summary>
/// Provides view data for the DepartmentEditorPage screen or form.
/// </summary>
public sealed class DepartmentEditorPageViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Department Name is required")]
    [StringLength(100)]
    [Display(Name = "Department Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public List<int> SelectedCompanyIds { get; set; } = [];

    public List<PageCompanyOption> CompanyOptions { get; set; } = [];
}

namespace IT_Form.Application.Features.Company;

public sealed class CompanyIndexListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Lot { get; init; }
    public string? LogoPath { get; init; }
    public bool IsLoginPageLogo { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CompanyEditorPageViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Lot { get; set; }
    public string? LogoPath { get; set; }
    public bool IsLoginPageLogo { get; set; }
    public bool IsActive { get; set; } = true;
}

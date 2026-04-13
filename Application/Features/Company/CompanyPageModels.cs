using IT_Form.Helpers;

namespace IT_Form.Application.Features.Company;

public sealed class CompanyIndexListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Lot { get; init; }
    public string? LogoPath { get; init; }
    public bool IsLoginPageLogo { get; init; }
    public bool IsActive { get; init; }
    public string TimeZoneId { get; init; } = CompanyTimeZoneHelper.DefaultTimeZoneId;
    public TimeSpan BusinessStartTime { get; init; } = new(8, 30, 0);
    public TimeSpan BusinessEndTime { get; init; } = new(17, 30, 0);
    public bool WorksOnMonday { get; init; } = true;
    public bool WorksOnTuesday { get; init; } = true;
    public bool WorksOnWednesday { get; init; } = true;
    public bool WorksOnThursday { get; init; } = true;
    public bool WorksOnFriday { get; init; } = true;
    public bool WorksOnSaturday { get; init; } = true;
    public bool WorksOnSunday { get; init; }
}

public sealed class CompanyEditorPageViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Lot { get; set; }
    public string? LogoPath { get; set; }
    public bool IsLoginPageLogo { get; set; }
    public bool IsActive { get; set; } = true;
    public string TimeZoneId { get; set; } = CompanyTimeZoneHelper.DefaultTimeZoneId;
    public TimeSpan BusinessStartTime { get; set; } = new(8, 30, 0);
    public TimeSpan BusinessEndTime { get; set; } = new(17, 30, 0);
    public bool WorksOnMonday { get; set; } = true;
    public bool WorksOnTuesday { get; set; } = true;
    public bool WorksOnWednesday { get; set; } = true;
    public bool WorksOnThursday { get; set; } = true;
    public bool WorksOnFriday { get; set; } = true;
    public bool WorksOnSaturday { get; set; } = true;
    public bool WorksOnSunday { get; set; }
}

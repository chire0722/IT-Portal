using IT_Form.Application.Common.Models;

namespace IT_Form.Application.Features.StandardSoftware;

public sealed class StandardSoftwareIndexPageViewModel
{
    public int TotalSoftwareCount { get; set; }
    public int TotalVersionCount { get; set; }
    public int TotalSeatCapacity { get; set; }
    public int TotalAssignedSeats { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public string? CurrentSearch { get; set; }
    public bool? IsMandatory { get; set; }
    public bool? IsSubscription { get; set; }
    public int? SelectedCompanyId { get; set; }
    public List<PageCompanyOption> CompanyOptions { get; set; } = [];
    public List<StandardSoftwareIndexItem> SoftwareList { get; set; } = [];

    public double UtilizationPercentage => TotalSeatCapacity > 0
        ? (double)TotalAssignedSeats / TotalSeatCapacity * 100
        : 0;
}

public sealed class StandardSoftwareIndexItem
{
    public int Id { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }
    public bool HasMultipleVersions { get; set; }
    public List<StandardSoftwareLicenseListItem> Licenses { get; set; } = [];
}

public sealed class StandardSoftwareLicenseListItem
{
    public int Id { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public int ContractLimit { get; set; }
    public string? Description { get; set; }
    public string? VendorName { get; set; }
    public string? SupportContact { get; set; }
    public bool IsSubscription { get; set; }
    public bool IsFree { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int KeyCount { get; set; }
    public List<int> CompanyIds { get; set; } = [];
    public List<string> Companies { get; set; } = [];
}

public sealed class StandardSoftwareEditorPageViewModel
{
    public int Id { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasMultipleVersions { get; set; }
}

public sealed class StandardSoftwareKeysResponseDto
{
    public bool IsSubscription { get; set; }
    public int ContractLimit { get; set; }
    public List<StandardSoftwareKeyDto> Keys { get; set; } = [];
}

public sealed class StandardSoftwareKeyDto
{
    public int Id { get; set; }
    public string KeyValue { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }
    public string? AssignedTo { get; set; }
    public int? OnboardingRequestId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class StandardSoftwareEmployeeOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

using IT_Form.Models.Enums;

namespace IT_Form.Application.DTOs.Options;

public sealed class SoftwareLicenseOptionDto
{
    public int Id { get; set; }
    public int StandardSoftwareId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public bool IsSubscription { get; set; }
    public bool IsFree { get; set; }
    public int ContractLimit { get; set; }
}

public sealed class StandardSoftwareOptionDto
{
    public int Id { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public List<SoftwareLicenseOptionDto> Licenses { get; set; } = [];
}

public sealed class EmailGroupOptionDto
{
    public int Id { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string GroupEmail { get; set; } = string.Empty;
}

public sealed class SharedMailboxOptionDto
{
    public int Id { get; set; }
    public string MailboxName { get; set; } = string.Empty;
    public string MailboxAddress { get; set; } = string.Empty;
}

public sealed class ITAssetOptionDto
{
    public int Id { get; set; }
    public AssetType AssetType { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
}

// Project comment: DepartmentAssetDto.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Department.GetDepartmentAssets;

/// <summary>
/// Defines the DepartmentAssetDto class used by the IT Form application.
/// </summary>
public sealed class DepartmentAssetDto
{
    public string EmployeeName { get; init; } = string.Empty;
    public string AssetName { get; init; } = string.Empty;
    public string SerialNumber { get; init; } = string.Empty;
}

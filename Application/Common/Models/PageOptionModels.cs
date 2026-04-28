// Project comment: PageOptionModels.cs is part of the IT Form application source code.

namespace IT_Form.Application.Common.Models;

/// <summary>
/// Defines the PageCompanyOption class used by the IT Form application.
/// </summary>
public sealed class PageCompanyOption
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Defines the PageCurrentEmployeeOption class used by the IT Form application.
/// </summary>
public sealed class PageCurrentEmployeeOption
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
}

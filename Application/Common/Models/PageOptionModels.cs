namespace IT_Form.Application.Common.Models;

public sealed class PageCompanyOption
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class PageCurrentEmployeeOption
{
    public int Id { get; init; }
    public string Label { get; init; } = string.Empty;
}

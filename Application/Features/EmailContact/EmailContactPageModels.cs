using System.Text.Json.Serialization;

namespace IT_Form.Application.Features.EmailContact;

public sealed class EmailContactIndexListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string? Department { get; init; }
}

public sealed class EmailContactEditorPageViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class EmailContactSearchItem
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("department")]
    public string? Department { get; init; }
}

// Project comment: EmailContactPageModels.cs is part of the IT Form application source code.

using System.Text.Json.Serialization;

namespace IT_Form.Application.Features.EmailContact;

/// <summary>
/// Defines the EmailContactIndexListItem class used by the IT Form application.
/// </summary>
public sealed class EmailContactIndexListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string? Department { get; init; }
}

/// <summary>
/// Provides view data for the EmailContactEditorPage screen or form.
/// </summary>
public sealed class EmailContactEditorPageViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Defines the EmailContactSearchItem class used by the IT Form application.
/// </summary>
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

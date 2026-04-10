namespace IT_Form.ViewModels;

public sealed class SignatureAcknowledgePageViewModel
{
    public Guid Token { get; init; }
    public string RequestType { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Department { get; init; }
    public string? Company { get; init; }
    public bool AlreadySigned { get; init; }
    public bool Expired { get; init; }
}

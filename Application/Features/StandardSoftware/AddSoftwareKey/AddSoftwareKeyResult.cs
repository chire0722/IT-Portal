namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareKey
{
    public enum AddSoftwareKeyStatus
    {
        Added,
        Failed
    }

    public sealed record AddSoftwareKeyResult(
        AddSoftwareKeyStatus Status,
        string Message);
}

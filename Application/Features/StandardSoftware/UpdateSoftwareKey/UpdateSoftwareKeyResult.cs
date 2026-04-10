namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    public enum UpdateSoftwareKeyStatus
    {
        Updated,
        NotFound
    }

    public sealed record UpdateSoftwareKeyResult(
        UpdateSoftwareKeyStatus Status,
        string Message);
}

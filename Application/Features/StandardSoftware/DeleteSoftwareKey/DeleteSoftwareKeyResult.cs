namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    public enum DeleteSoftwareKeyStatus
    {
        Deleted,
        NotFound,
        AssignedBlocked
    }

    public sealed record DeleteSoftwareKeyResult(
        DeleteSoftwareKeyStatus Status,
        string Message);
}

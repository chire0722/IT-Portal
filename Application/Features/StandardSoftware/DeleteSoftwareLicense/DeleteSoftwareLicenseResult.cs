namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    public enum DeleteSoftwareLicenseStatus
    {
        Deleted,
        NotFound,
        HasAssignedKeys
    }

    public sealed record DeleteSoftwareLicenseResult(
        DeleteSoftwareLicenseStatus Status,
        string Message);
}

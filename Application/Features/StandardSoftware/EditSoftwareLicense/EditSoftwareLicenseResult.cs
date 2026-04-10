namespace IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense
{
    public enum EditSoftwareLicenseStatus
    {
        Updated,
        NotFound,
        InvalidSubscriptionExpiry,
        InvalidSeatLimit
    }

    public sealed record EditSoftwareLicenseResult(
        EditSoftwareLicenseStatus Status,
        string Message);
}

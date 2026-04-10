namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense
{
    public enum AddSoftwareLicenseStatus
    {
        Added,
        NotFound,
        InvalidSubscriptionExpiry
    }

    public sealed record AddSoftwareLicenseResult(
        AddSoftwareLicenseStatus Status,
        string Message);
}

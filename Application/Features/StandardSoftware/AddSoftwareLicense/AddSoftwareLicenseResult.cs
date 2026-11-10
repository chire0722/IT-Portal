// Project comment: AddSoftwareLicenseResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense
{
    /// <summary>
    /// Lists the supported AddSoftwareLicenseStatus values used by the application.
    /// </summary>
    public enum AddSoftwareLicenseStatus
    {
        Added,
        NotFound,
        InvalidSubscriptionExpiry,
        InvalidSeatLimit
    }

    /// <summary>
    /// Represents the result returned by the AddSoftwareLicense workflow.
    /// </summary>
    public sealed record AddSoftwareLicenseResult(
        AddSoftwareLicenseStatus Status,
        string Message);
}

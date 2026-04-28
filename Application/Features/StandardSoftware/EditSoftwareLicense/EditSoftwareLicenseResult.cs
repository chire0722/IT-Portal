// Project comment: EditSoftwareLicenseResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense
{
    /// <summary>
    /// Lists the supported EditSoftwareLicenseStatus values used by the application.
    /// </summary>
    public enum EditSoftwareLicenseStatus
    {
        Updated,
        NotFound,
        InvalidSubscriptionExpiry,
        InvalidSeatLimit
    }

    /// <summary>
    /// Represents the result returned by the EditSoftwareLicense workflow.
    /// </summary>
    public sealed record EditSoftwareLicenseResult(
        EditSoftwareLicenseStatus Status,
        string Message);
}

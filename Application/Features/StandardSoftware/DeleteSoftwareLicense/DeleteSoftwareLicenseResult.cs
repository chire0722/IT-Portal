// Project comment: DeleteSoftwareLicenseResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense
{
    /// <summary>
    /// Lists the supported DeleteSoftwareLicenseStatus values used by the application.
    /// </summary>
    public enum DeleteSoftwareLicenseStatus
    {
        Deleted,
        NotFound,
        HasAssignedKeys
    }

    /// <summary>
    /// Represents the result returned by the DeleteSoftwareLicense workflow.
    /// </summary>
    public sealed record DeleteSoftwareLicenseResult(
        DeleteSoftwareLicenseStatus Status,
        string Message);
}

// Project comment: UpdateSoftwareKeyResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey
{
    /// <summary>
    /// Lists the supported UpdateSoftwareKeyStatus values used by the application.
    /// </summary>
    public enum UpdateSoftwareKeyStatus
    {
        Updated,
        NotFound
    }

    /// <summary>
    /// Represents the result returned by the UpdateSoftwareKey workflow.
    /// </summary>
    public sealed record UpdateSoftwareKeyResult(
        UpdateSoftwareKeyStatus Status,
        string Message);
}

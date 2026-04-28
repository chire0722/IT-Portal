// Project comment: DeleteSoftwareKeyResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey
{
    /// <summary>
    /// Lists the supported DeleteSoftwareKeyStatus values used by the application.
    /// </summary>
    public enum DeleteSoftwareKeyStatus
    {
        Deleted,
        NotFound,
        AssignedBlocked
    }

    /// <summary>
    /// Represents the result returned by the DeleteSoftwareKey workflow.
    /// </summary>
    public sealed record DeleteSoftwareKeyResult(
        DeleteSoftwareKeyStatus Status,
        string Message);
}

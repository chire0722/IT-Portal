// Project comment: AssignSoftwareKeyResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    /// <summary>
    /// Lists the supported AssignSoftwareKeyStatus values used by the application.
    /// </summary>
    public enum AssignSoftwareKeyStatus
    {
        AssignedOrUnassigned,
        NotFound,
        EmployeeNotFound
    }

    /// <summary>
    /// Represents the result returned by the AssignSoftwareKey workflow.
    /// </summary>
    public sealed record AssignSoftwareKeyResult(
        AssignSoftwareKeyStatus Status,
        string Message);
}

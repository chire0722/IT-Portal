// Project comment: AddCurrentEmployeeToEmailGroupResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    /// <summary>
    /// Lists the supported AddCurrentEmployeeToEmailGroupStatus values used by the application.
    /// </summary>
    public enum AddCurrentEmployeeToEmailGroupStatus
    {
        Added,
        GroupNotFound,
        InvalidEmployee,
        AlreadyExists,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the AddCurrentEmployeeToEmailGroup workflow.
    /// </summary>
    public sealed record AddCurrentEmployeeToEmailGroupResult(
        AddCurrentEmployeeToEmailGroupStatus Status,
        string Message);
}

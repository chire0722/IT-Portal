// Project comment: AddCurrentEmployeeToSharedMailboxResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    /// <summary>
    /// Lists the supported AddCurrentEmployeeToSharedMailboxStatus values used by the application.
    /// </summary>
    public enum AddCurrentEmployeeToSharedMailboxStatus
    {
        Added,
        MailboxNotFound,
        InvalidEmployee,
        AlreadyExists,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the AddCurrentEmployeeToSharedMailbox workflow.
    /// </summary>
    public sealed record AddCurrentEmployeeToSharedMailboxResult(
        AddCurrentEmployeeToSharedMailboxStatus Status,
        string Message);
}

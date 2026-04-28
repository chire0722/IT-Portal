// Project comment: AddCurrentEmployeeToServerFolderResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    /// <summary>
    /// Lists the supported AddCurrentEmployeeToServerFolderStatus values used by the application.
    /// </summary>
    public enum AddCurrentEmployeeToServerFolderStatus
    {
        Added,
        Updated,
        FolderNotFound,
        InvalidEmployee,
        Forbidden
    }

    /// <summary>
    /// Represents the result returned by the AddCurrentEmployeeToServerFolder workflow.
    /// </summary>
    public sealed record AddCurrentEmployeeToServerFolderResult(
        AddCurrentEmployeeToServerFolderStatus Status,
        string Message);
}

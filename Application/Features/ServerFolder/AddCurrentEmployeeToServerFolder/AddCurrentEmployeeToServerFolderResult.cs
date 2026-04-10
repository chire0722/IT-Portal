namespace IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder
{
    public enum AddCurrentEmployeeToServerFolderStatus
    {
        Added,
        Updated,
        FolderNotFound,
        InvalidEmployee,
        Forbidden
    }

    public sealed record AddCurrentEmployeeToServerFolderResult(
        AddCurrentEmployeeToServerFolderStatus Status,
        string Message);
}

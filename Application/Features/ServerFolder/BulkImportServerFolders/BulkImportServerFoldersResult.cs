namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
    public sealed record BulkImportServerFoldersResult(
        bool Succeeded,
        string? SuccessMessage,
        string? InfoMessage,
        string? ErrorMessage);
}

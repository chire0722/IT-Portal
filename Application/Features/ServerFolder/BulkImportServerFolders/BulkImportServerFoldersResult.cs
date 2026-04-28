// Project comment: BulkImportServerFoldersResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.ServerFolder.BulkImportServerFolders
{
    /// <summary>
    /// Represents the result returned by the BulkImportServerFolders workflow.
    /// </summary>
    public sealed record BulkImportServerFoldersResult(
        bool Succeeded,
        string? SuccessMessage,
        string? InfoMessage,
        string? ErrorMessage);
}

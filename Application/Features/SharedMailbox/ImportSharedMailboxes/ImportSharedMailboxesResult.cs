// Project comment: ImportSharedMailboxesResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    /// <summary>
    /// Represents the result returned by the ImportSharedMailboxes workflow.
    /// </summary>
    public sealed record ImportSharedMailboxesResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

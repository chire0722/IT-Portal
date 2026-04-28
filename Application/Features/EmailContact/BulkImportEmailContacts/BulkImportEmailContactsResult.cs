// Project comment: BulkImportEmailContactsResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    /// <summary>
    /// Represents the result returned by the BulkImportEmailContacts workflow.
    /// </summary>
    public sealed record BulkImportEmailContactsResult(
        bool Succeeded,
        string? SuccessMessage,
        string? InfoMessage,
        string? ErrorMessage);
}

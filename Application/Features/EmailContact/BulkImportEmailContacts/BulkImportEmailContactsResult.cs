namespace IT_Form.Application.Features.EmailContact.BulkImportEmailContacts
{
    public sealed record BulkImportEmailContactsResult(
        bool Succeeded,
        string? SuccessMessage,
        string? InfoMessage,
        string? ErrorMessage);
}

namespace IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes
{
    public sealed record ImportSharedMailboxesResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

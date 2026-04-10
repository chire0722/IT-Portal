namespace IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox
{
    public enum AddCurrentEmployeeToSharedMailboxStatus
    {
        Added,
        MailboxNotFound,
        InvalidEmployee,
        AlreadyExists,
        Forbidden
    }

    public sealed record AddCurrentEmployeeToSharedMailboxResult(
        AddCurrentEmployeeToSharedMailboxStatus Status,
        string Message);
}

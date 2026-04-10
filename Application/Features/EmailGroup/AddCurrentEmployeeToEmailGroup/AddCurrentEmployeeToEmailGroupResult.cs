namespace IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup
{
    public enum AddCurrentEmployeeToEmailGroupStatus
    {
        Added,
        GroupNotFound,
        InvalidEmployee,
        AlreadyExists,
        Forbidden
    }

    public sealed record AddCurrentEmployeeToEmailGroupResult(
        AddCurrentEmployeeToEmailGroupStatus Status,
        string Message);
}

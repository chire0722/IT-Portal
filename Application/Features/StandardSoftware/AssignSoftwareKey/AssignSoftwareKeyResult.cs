namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    public enum AssignSoftwareKeyStatus
    {
        AssignedOrUnassigned,
        NotFound,
        EmployeeNotFound
    }

    public sealed record AssignSoftwareKeyResult(
        AssignSoftwareKeyStatus Status,
        string Message);
}

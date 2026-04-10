namespace IT_Form.Application.Features.Department.ImportDepartments
{
    public sealed record ImportDepartmentsResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

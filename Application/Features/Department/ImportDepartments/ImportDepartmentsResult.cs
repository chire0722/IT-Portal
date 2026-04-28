// Project comment: ImportDepartmentsResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Department.ImportDepartments
{
    /// <summary>
    /// Represents the result returned by the ImportDepartments workflow.
    /// </summary>
    public sealed record ImportDepartmentsResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

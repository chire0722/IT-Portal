// Project comment: ImportStandardSoftwareResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    /// <summary>
    /// Represents the result returned by the ImportStandardSoftware workflow.
    /// </summary>
    public sealed record ImportStandardSoftwareResult(
        bool Succeeded,
        string? SuccessMessage,
        string? ErrorMessage);
}

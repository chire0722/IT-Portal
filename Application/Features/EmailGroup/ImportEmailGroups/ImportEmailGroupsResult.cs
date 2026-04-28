// Project comment: ImportEmailGroupsResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.EmailGroup.ImportEmailGroups
{
    /// <summary>
    /// Represents the result returned by the ImportEmailGroups workflow.
    /// </summary>
    public sealed record ImportEmailGroupsResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

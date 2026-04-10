namespace IT_Form.Application.Features.EmailGroup.ImportEmailGroups
{
    public sealed record ImportEmailGroupsResult(
        bool Succeeded,
        string SuccessMessage,
        string? ErrorMessage);
}

namespace IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware
{
    public sealed record ImportStandardSoftwareResult(
        bool Succeeded,
        string? SuccessMessage,
        string? ErrorMessage);
}

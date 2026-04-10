namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    public enum UpdateManageAccessStatus
    {
        Updated,
        NotFound,
        InvalidState
    }

    public sealed record UpdateManageAccessResult(
        UpdateManageAccessStatus Status,
        int RequestId,
        string Message);
}

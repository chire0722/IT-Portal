// Project comment: UpdateManageAccessResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.UpdateManageAccess
{
    /// <summary>
    /// Lists the supported UpdateManageAccessStatus values used by the application.
    /// </summary>
    public enum UpdateManageAccessStatus
    {
        Updated,
        NotFound,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the UpdateManageAccess workflow.
    /// </summary>
    public sealed record UpdateManageAccessResult(
        UpdateManageAccessStatus Status,
        int RequestId,
        string Message);
}

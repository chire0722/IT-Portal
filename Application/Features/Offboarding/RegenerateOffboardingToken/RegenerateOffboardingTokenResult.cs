// Project comment: RegenerateOffboardingTokenResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    /// <summary>
    /// Lists the supported RegenerateOffboardingTokenStatus values used by the application.
    /// </summary>
    public enum RegenerateOffboardingTokenStatus
    {
        Regenerated,
        NotFound,
        ForbiddenRole,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the RegenerateOffboardingToken workflow.
    /// </summary>
    public sealed record RegenerateOffboardingTokenResult(
        RegenerateOffboardingTokenStatus Status,
        int RequestId,
        string Message);
}

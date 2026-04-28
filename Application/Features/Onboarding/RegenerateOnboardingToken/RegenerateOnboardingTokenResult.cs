// Project comment: RegenerateOnboardingTokenResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    /// <summary>
    /// Lists the supported RegenerateOnboardingTokenStatus values used by the application.
    /// </summary>
    public enum RegenerateOnboardingTokenStatus
    {
        Regenerated,
        NotFound,
        ForbiddenScope,
        InvalidState
    }

    /// <summary>
    /// Represents the result returned by the RegenerateOnboardingToken workflow.
    /// </summary>
    public sealed record RegenerateOnboardingTokenResult(
        RegenerateOnboardingTokenStatus Status,
        int RequestId,
        string Message);
}

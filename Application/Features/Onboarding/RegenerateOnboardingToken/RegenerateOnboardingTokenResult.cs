namespace IT_Form.Application.Features.Onboarding.RegenerateOnboardingToken
{
    public enum RegenerateOnboardingTokenStatus
    {
        Regenerated,
        NotFound,
        ForbiddenScope,
        InvalidState
    }

    public sealed record RegenerateOnboardingTokenResult(
        RegenerateOnboardingTokenStatus Status,
        int RequestId,
        string Message);
}

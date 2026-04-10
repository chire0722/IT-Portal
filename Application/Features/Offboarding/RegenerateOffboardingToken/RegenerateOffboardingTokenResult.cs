namespace IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken
{
    public enum RegenerateOffboardingTokenStatus
    {
        Regenerated,
        NotFound,
        ForbiddenRole,
        InvalidState
    }

    public sealed record RegenerateOffboardingTokenResult(
        RegenerateOffboardingTokenStatus Status,
        int RequestId,
        string Message);
}

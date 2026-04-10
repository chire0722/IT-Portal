namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    public enum CompleteOnboardingItSetupStatus
    {
        DraftSaved,
        Completed,
        NotFound,
        ChecklistIncomplete
    }

    public sealed record CompleteOnboardingItSetupResult(
        CompleteOnboardingItSetupStatus Status,
        int RequestId,
        string Message,
        string? WarningMessage = null);
}


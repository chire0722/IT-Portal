// Project comment: CompleteOnboardingItSetupResult.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    /// <summary>
    /// Describes the outcome of an IT setup completion request.
    /// </summary>
    public enum CompleteOnboardingItSetupStatus
    {
        /// <summary>
        /// The checklist was saved without completing the onboarding request.
        /// </summary>
        DraftSaved,

        /// <summary>
        /// The IT setup was completed and persisted successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The onboarding request referenced by the session could not be found.
        /// </summary>
        NotFound,

        /// <summary>
        /// The request was submitted for completion before all required checklist items were checked.
        /// </summary>
        ChecklistIncomplete,

        /// <summary>
        /// The selected inventory assets are missing, unavailable, duplicated, or outside the request company scope.
        /// </summary>
        InvalidAssetSelection,

        /// <summary>
        /// The request is not in the IT setup state required for this operation.
        /// </summary>
        InvalidState,

        /// <summary>
        /// The current user is not allowed to perform this operation for the request company.
        /// </summary>
        Forbidden
    }

    /// <summary>
    /// Response returned after saving or completing the onboarding IT setup step.
    /// </summary>
    /// <param name="Status">The machine-readable result status.</param>
    /// <param name="RequestId">The onboarding request id involved in the operation.</param>
    /// <param name="Message">The user-facing success or failure message.</param>
    /// <param name="WarningMessage">Optional warning text, such as out-of-stock software notices.</param>
    public sealed record CompleteOnboardingItSetupResult(
        CompleteOnboardingItSetupStatus Status,
        int RequestId,
        string Message,
        string? WarningMessage = null);
}


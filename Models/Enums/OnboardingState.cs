// Project comment: OnboardingState.cs is part of the IT Form application source code.

namespace IT_Form.Models.Enums;

/// <summary>
/// Lists the supported OnboardingState values used by the application.
/// </summary>
public enum OnboardingState
{
    Draft = 0,
    HrSubmitted = 1,
    ItProvisioning = 2,
    Completed = 3,
    Canceled = 4
}

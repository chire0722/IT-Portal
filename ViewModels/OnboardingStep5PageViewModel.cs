// Project comment: OnboardingStep5PageViewModel.cs is part of the IT Form application source code.

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OnboardingStep5Page screen or form.
    /// </summary>
    public sealed class OnboardingStep5PageViewModel : OnboardingViewModel
    {
        public int RequestId { get; set; }
        public string? CurrentUserSignature { get; set; }
    }
}

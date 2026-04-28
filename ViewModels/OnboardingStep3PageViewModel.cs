// Project comment: OnboardingStep3PageViewModel.cs is part of the IT Form application source code.

using IT_Form.Application.DTOs.Options;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OnboardingStep3Page screen or form.
    /// </summary>
    public sealed class OnboardingStep3PageViewModel : OnboardingViewModel
    {
        public int RequestId { get; set; }
        public List<SoftwareLicenseOptionDto> AvailableSoftware { get; set; } = new();
        public List<StandardSoftwareOptionDto> StandardSoftwareOptions { get; set; } = new();
        public Dictionary<int, int> LicenseAvailability { get; set; } = new();
        public List<int> RequestedSoftwareIds { get; set; } = new();
        public List<EmailGroupOptionDto> EmailGroups { get; set; } = new();
        public List<SharedMailboxOptionDto> SharedMailboxes { get; set; } = new();
        public List<ITAssetOptionDto> AvailableAssets { get; set; } = new();
    }
}

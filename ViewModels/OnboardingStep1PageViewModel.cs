// Project comment: OnboardingStep1PageViewModel.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OnboardingStep1Page screen or form.
    /// </summary>
    public sealed class OnboardingStep1PageViewModel : OnboardingViewModel
    {
        public List<string> AllowedCompanies { get; set; } = new();
        public List<OnboardingDepartmentOptionViewModel> Departments { get; set; } = new();
        public IEnumerable<SelectListItem> HODUsers { get; set; } = Enumerable.Empty<SelectListItem>();
    }

    /// <summary>
    /// Provides view data for the OnboardingDepartmentOption screen or form.
    /// </summary>
    public sealed class OnboardingDepartmentOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
    }
}

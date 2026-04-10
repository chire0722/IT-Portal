using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.ViewModels
{
    public sealed class OnboardingStep1PageViewModel : OnboardingViewModel
    {
        public List<string> AllowedCompanies { get; set; } = new();
        public List<OnboardingDepartmentOptionViewModel> Departments { get; set; } = new();
        public IEnumerable<SelectListItem> HODUsers { get; set; } = Enumerable.Empty<SelectListItem>();
    }

    public sealed class OnboardingDepartmentOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
    }
}

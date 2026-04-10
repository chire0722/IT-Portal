using IT_Form.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.ViewModels
{
    public sealed class EditOnboardingPageViewModel : OnboardingRequest
    {
        public List<StandardSoftware> StandardSoftwareOptions { get; set; } = new();
        public List<EmailGroup> EmailGroupOptions { get; set; } = new();
        public List<SharedMailbox> SharedMailboxOptions { get; set; } = new();
        public List<string> CompanyOptions { get; set; } = new();
        public List<Department> DepartmentOptions { get; set; } = new();
        public IEnumerable<SelectListItem> HODUserOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}

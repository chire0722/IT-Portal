using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.ViewModels
{
    public sealed class OffboardingCreatePageViewModel : IT_Form.Models.OffboardingRequest
    {
        public IEnumerable<SelectListItem> Employees { get; set; } = [];
        public IEnumerable<SelectListItem> AssignedHodList { get; set; } = [];
    }
}

// Project comment: OffboardingCreatePageViewModel.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// Provides view data for the OffboardingCreatePage screen or form.
    /// </summary>
    public sealed class OffboardingCreatePageViewModel : IT_Form.Models.OffboardingRequest
    {
        public IEnumerable<SelectListItem> Employees { get; set; } = [];
        public IEnumerable<SelectListItem> AssignedHodList { get; set; } = [];
    }
}

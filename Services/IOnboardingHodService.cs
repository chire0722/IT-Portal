// Project comment: IOnboardingHodService.cs is part of the IT Form application source code.

using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides IOnboardingHod service behavior for the application.
    /// </summary>
    public interface IOnboardingHodService
    {
        Task<SelectList> BuildHodSelectListAsync(string? company, string? selectedHodEmail = null, CancellationToken cancellationToken = default);
        Task<bool> IsValidHodForCompanyAsync(string? company, string? hodEmail, CancellationToken cancellationToken = default);
    }
}

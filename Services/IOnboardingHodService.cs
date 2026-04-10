using Microsoft.AspNetCore.Mvc.Rendering;

namespace IT_Form.Services
{
    public interface IOnboardingHodService
    {
        Task<SelectList> BuildHodSelectListAsync(string? company, string? selectedHodEmail = null, CancellationToken cancellationToken = default);
        Task<bool> IsValidHodForCompanyAsync(string? company, string? hodEmail, CancellationToken cancellationToken = default);
    }
}

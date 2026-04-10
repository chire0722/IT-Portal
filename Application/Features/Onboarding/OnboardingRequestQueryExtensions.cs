using IT_Form.Models;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.Onboarding
{
    internal static class OnboardingRequestQueryExtensions
    {
        public static IQueryable<OnboardingRequest> ApplySearchFilter(
            this IQueryable<OnboardingRequest> query,
            string? searchString,
            RequestStatus? status)
        {
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var lowerSearch = searchString.ToLower();
                query = query.Where(r =>
                    (r.FullName != null && r.FullName.ToLower().Contains(lowerSearch)) ||
                    (r.EmployeeID != null && r.EmployeeID.ToLower().Contains(lowerSearch)) ||
                    (r.Department != null && r.Department.ToLower().Contains(lowerSearch)));
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            return query;
        }
    }
}

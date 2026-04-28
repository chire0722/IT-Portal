// Project comment: OnboardingRequestQueryExtensions.cs is part of the IT Form application source code.

using IT_Form.Models;
using IT_Form.Models.Enums;

namespace IT_Form.Application.Features.Onboarding
{
    /// <summary>
    /// Defines the OnboardingRequestQueryExtensions class used by the IT Form application.
    /// </summary>
    internal static class OnboardingRequestQueryExtensions
    {
        /// <summary>
        /// Applies changes for the ApplySearchFilter operation.
        /// </summary>
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

using System.Security.Claims;
using IT_Form.Models;

namespace IT_Form.Helpers;

public static class OnboardingRequestAuthorizationHelper
{
    public static bool CanDelete(ClaimsPrincipal? user, OnboardingRequest request)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var isHr = user.HasClaim("UserTitle", "HR");
        var isIt = user.HasClaim("UserTitle", "IT");

        return (isHr || isIt)
            && CompanyScopeHelper.CanAccessCompany(user, request.Branch)
            && request.CanBeDeletedBy(isHr);
    }
}

// Project comment: OffboardingActionWriteHelpers.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IT_Form.Application.Features.Offboarding.ActionWrites
{
    /// <summary>
    /// Defines the OffboardingActionWriteHelpers class used by the IT Form application.
    /// </summary>
    internal static class OffboardingActionWriteHelpers
    {
        /// <summary>
        /// Runs the IsUserAuthorizedForRequest operation.
        /// </summary>
        internal static async Task<bool> IsUserAuthorizedForRequest(
            ApplicationDbContext context,
            ClaimsPrincipal user,
            UserManager<ApplicationUser> userManager,
            OffboardingRequest request,
            string? userName)
        {
            if (!CompanyScopeHelper.CanAccessCompany(user, request.Branch)) return false;
            if (user.HasClaim("UserTitle", "IT")) return true;
            if (user.HasClaim("UserTitle", "HR")) return true;

            var username = userName ?? string.Empty;
            if (request.CreatedByUserId == username) return true;

            var userId = userManager.GetUserId(user);
            if (!string.IsNullOrWhiteSpace(request.AssignedHodId))
                return request.AssignedHodId == userId;

            var hodEmailsLower = await UserHelper.GetHODEmailsAsync(context, username);
            return request.HODEmail != null && hodEmailsLower.Contains(request.HODEmail.ToLowerInvariant());
        }

    }
}

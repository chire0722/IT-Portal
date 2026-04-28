// Project comment: OffboardingReadAccessSupport.cs is part of the IT Form application source code.

using System.Security.Claims;
using IT_Form.Application.Common.Support;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Microsoft.AspNetCore.Identity;

namespace IT_Form.Application.Features.Offboarding;

/// <summary>
/// Defines the OffboardingReadAccessSupport class used by the IT Form application.
/// </summary>
internal static class OffboardingReadAccessSupport
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
        if (!HrRecordReadCompanyScopeSupport.CanAccessCompany(user, request.Branch))
        {
            return false;
        }

        if (user.HasClaim("UserTitle", "IT") || user.HasClaim("UserTitle", "HR"))
        {
            return true;
        }

        var username = userName ?? string.Empty;
        if (request.CreatedByUserId == username)
        {
            return true;
        }

        var userId = userManager.GetUserId(user);
        if (!string.IsNullOrWhiteSpace(request.AssignedHodId))
        {
            return request.AssignedHodId == userId;
        }

        var hodEmailsLower = await UserHelper.GetHODEmailsAsync(context, username);
        return request.HODEmail != null && hodEmailsLower.Contains(request.HODEmail.ToLowerInvariant());
    }
}

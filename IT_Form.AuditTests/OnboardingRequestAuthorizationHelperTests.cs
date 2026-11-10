using System.Security.Claims;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;

namespace IT_Form.AuditTests;

public sealed class OnboardingRequestAuthorizationHelperTests
{
    [Fact]
    public void CanDelete_DeniesOrdinaryEmployee_EvenWhenRequestIsDraft()
    {
        var user = CreateUser("Employee", "Company A");
        var request = CreateRequest("Company A", RequestStatus.Draft);

        Assert.False(OnboardingRequestAuthorizationHelper.CanDelete(user, request));
    }

    [Fact]
    public void CanDelete_DeniesHrFromAnotherCompany()
    {
        var user = CreateUser("HR", "Company B");
        var request = CreateRequest("Company A", RequestStatus.Draft);

        Assert.False(OnboardingRequestAuthorizationHelper.CanDelete(user, request));
    }

    [Fact]
    public void CanDelete_AllowsScopedItToDeleteDraft()
    {
        var user = CreateUser("IT", "Company A");
        var request = CreateRequest("Company A", RequestStatus.Draft);

        Assert.True(OnboardingRequestAuthorizationHelper.CanDelete(user, request));
    }

    [Fact]
    public void CanDelete_AllowsHrButNotItToDeleteHrSignedPendingRequest()
    {
        var hr = CreateUser("HR", "Company A");
        var it = CreateUser("IT", "Company A");
        var request = CreateRequest("Company A", RequestStatus.PendingITApproval);
        request.RequesterSignature = "synthetic-signature";

        Assert.True(OnboardingRequestAuthorizationHelper.CanDelete(hr, request));
        Assert.False(OnboardingRequestAuthorizationHelper.CanDelete(it, request));
    }

    [Fact]
    public void CanDelete_DeniesScopedRoleWhenCompanyClaimIsMissing()
    {
        var user = CreateUser("IT", company: null);
        var request = CreateRequest("Company A", RequestStatus.Draft);

        Assert.False(OnboardingRequestAuthorizationHelper.CanDelete(user, request));
    }

    private static ClaimsPrincipal CreateUser(string title, string? company)
    {
        var claims = new List<Claim>
        {
            new("UserTitle", title),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, $"test-{title.ToLowerInvariant()}")
        };

        if (!string.IsNullOrWhiteSpace(company))
        {
            claims.Add(new Claim("UserCompany", company));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "UnitTest"));
    }

    private static OnboardingRequest CreateRequest(string branch, RequestStatus status)
        => new()
        {
            Branch = branch,
            Status = status
        };
}
